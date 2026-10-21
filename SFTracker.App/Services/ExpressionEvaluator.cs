using System.Globalization;

namespace SFTracker.Services;

/// <summary>
/// Evaluates the small arithmetic the rate fields accept: numbers with "." decimals, + - * / with the
/// usual precedence, unary minus and whitespace. No parentheses, no functions. Incomplete input such as
/// "190-" simply fails, so callers can keep their last good value while the user is still typing.
/// </summary>
public static class ExpressionEvaluator {
	public static bool TryEvaluate(string? text, out double value) {
		value = 0;
		if (string.IsNullOrWhiteSpace(text)) {
			return false;
		}
		var parser = new Parser(text);
		if (!parser.TryParseSum(out value) || !parser.AtEnd || !double.IsFinite(value)) {
			value = 0;
			return false;
		}
		return true;
	}

	// sum := term (('+' | '-') term)* ; term := factor (('*' | '/') factor)* ; factor := '-' factor | number
	private ref struct Parser(string text) {
		private readonly string m_text = text;
		private int m_pos = 0;

		public bool AtEnd {
			get {
				SkipSpaces();
				return m_pos >= m_text.Length;
			}
		}

		public bool TryParseSum(out double value) {
			if (!TryParseTerm(out value)) {
				return false;
			}
			while (true) {
				SkipSpaces();
				if (!TryTakeOperator('+', '-', out var op)) {
					return true;
				}
				if (!TryParseTerm(out var rhs)) {
					return false;
				}
				value = op == '+' ? value + rhs : value - rhs;
			}
		}

		private bool TryParseTerm(out double value) {
			if (!TryParseFactor(out value)) {
				return false;
			}
			while (true) {
				SkipSpaces();
				if (!TryTakeOperator('*', '/', out var op)) {
					return true;
				}
				if (!TryParseFactor(out var rhs)) {
					return false;
				}
				value = op == '*' ? value * rhs : value / rhs;
			}
		}

		private bool TryParseFactor(out double value) {
			SkipSpaces();
			if (m_pos < m_text.Length && m_text[m_pos] == '-') {
				m_pos++;
				var ok = TryParseFactor(out value);
				value = -value;
				return ok;
			}
			var start = m_pos;
			while (m_pos < m_text.Length && (char.IsDigit(m_text[m_pos]) || m_text[m_pos] == '.')) {
				m_pos++;
			}
			return double.TryParse(m_text.AsSpan(start, m_pos - start), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
		}

		private bool TryTakeOperator(char a, char b, out char op) {
			op = default;
			if (m_pos < m_text.Length && (m_text[m_pos] == a || m_text[m_pos] == b)) {
				op = m_text[m_pos++];
				return true;
			}
			return false;
		}

		private void SkipSpaces() {
			while (m_pos < m_text.Length && char.IsWhiteSpace(m_text[m_pos])) {
				m_pos++;
			}
		}
	}
}
