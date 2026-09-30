using System.Globalization;

namespace Calculatrice; // Adaptez au nom de votre projet

public partial class MainPage : ContentPage
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private const int MaxDigits = 15;

    private string _current = "0";   // nombre en cours de saisie (séparateur interne : ".")
    private decimal? _left;          // opérande de gauche
    private string? _op;             // opérateur en attente
    private string _lastExpression = ""; // ex. "12 + 3 =" après un calcul
    private bool _newEntry;          // le prochain chiffre démarre un nouveau nombre
    private bool _error;             // état d'erreur (ex. division par zéro)

    public MainPage()
    {
        InitializeComponent();
        Refresh();
    }

    // ---------- Saisie ----------

    private void OnDigit(object? sender, EventArgs e)
    {
        if (_error) ResetAll();
        string d = ((Button)sender!).Text;

        if (_newEntry) { _current = "0"; _newEntry = false; _lastExpression = ""; }
        if (_current.Count(char.IsDigit) >= MaxDigits) return;

        _current = _current == "0" ? d : _current + d;
        Refresh();
    }

    private void OnDecimal(object? sender, EventArgs e)
    {
        if (_error) ResetAll();
        if (_newEntry) { _current = "0"; _newEntry = false; _lastExpression = ""; }
        if (!_current.Contains('.')) _current += ".";
        Refresh();
    }

    private void OnBackspace(object? sender, EventArgs e)
    {
        if (_error) { ResetAll(); Refresh(); return; }
        if (_newEntry) return; // on n'efface pas un résultat

        _current = _current.Length > 1 ? _current[..^1] : "0";
        if (_current == "-" || _current == "-0") _current = "0";
        Refresh();
    }

    private void OnClear(object? sender, EventArgs e)
    {
        ResetAll();
        Refresh();
    }

    private void OnSign(object? sender, EventArgs e)
    {
        if (_error || _current == "0") return;
        _current = _current.StartsWith('-') ? _current[1..] : "-" + _current;
        Refresh();
    }

    private void OnPercent(object? sender, EventArgs e)
    {
        if (_error) return;
        decimal value = Parse(_current);

        // "200 + 10 %" => 10 % de 200 ; sinon simple division par 100
        _current = Format(_op != null && _left.HasValue
            ? _left.Value * value / 100m
            : value / 100m);
        Refresh();
    }

    // ---------- Opérations ----------

    private void OnOperator(object? sender, EventArgs e)
    {
        if (_error) return;
        string op = ((Button)sender!).Text;

        if (_op != null && !_newEntry)
        {
            // enchaînement : 2 + 3 + ... => on calcule d'abord 2 + 3
            if (!Compute()) return;
        }
        else if (_op == null)
        {
            _left = Parse(_current);
        }
        // sinon : simple changement d'opérateur

        _op = op;
        _newEntry = true;
        _lastExpression = "";
        Refresh();
    }

    private void OnEquals(object? sender, EventArgs e)
    {
        if (_error || _op == null || !_left.HasValue) return;

        string expr = $"{Show(Format(_left.Value))} {_op} {Show(_current)} =";
        if (!Compute()) return;

        _lastExpression = expr;
        _op = null;
        _newEntry = true;
        Refresh();
    }

    /// <summary>Calcule _left (_op) _current. Retourne false en cas d'erreur.</summary>
    private bool Compute()
    {
        decimal right = Parse(_current);

        if (_op == "÷" && right == 0m)
        {
            ShowError("Division par zéro impossible");
            return false;
        }

        try
        {
            decimal result = _op switch
            {
                "+" => _left!.Value + right,
                "−" => _left!.Value - right,
                "×" => _left!.Value * right,
                "÷" => _left!.Value / right,
                _ => right
            };
            _left = result;
            _current = Format(result);
            return true;
        }
        catch (OverflowException)
        {
            ShowError("Nombre trop grand");
            return false;
        }
    }

    // ---------- Utilitaires ----------

    private void ResetAll()
    {
        _current = "0";
        _left = null;
        _op = null;
        _lastExpression = "";
        _newEntry = false;
        _error = false;
    }

    private void ShowError(string message)
    {
        _error = true;
        _op = null;
        _left = null;
        _lastExpression = "";
        ResultLabel.Text = message;
        ResultLabel.FontSize = 24;
        ExpressionLabel.Text = "";
    }

    private static decimal Parse(string s) => decimal.Parse(s, Inv);

    private static string Format(decimal value)
    {
        string s = value.ToString("0.############", Inv);
        // très grands nombres : notation scientifique pour rester lisible
        return s.Length > 18 ? ((double)value).ToString("G10", Inv) : s;
    }

    private static string Show(string internalValue) => internalValue.Replace('.', ',');

    private void Refresh()
    {
        if (_error) return;

        ResultLabel.Text = Show(_current);
        ResultLabel.FontSize = _current.Length switch
        {
            <= 8 => 64,
            <= 11 => 48,
            <= 14 => 38,
            _ => 30
        };

        ExpressionLabel.Text = _op != null
            ? $"{Show(Format(_left ?? 0))} {_op}"
            : _lastExpression;
    }
}
