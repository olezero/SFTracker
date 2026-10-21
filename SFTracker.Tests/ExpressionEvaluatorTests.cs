namespace SFTracker.Tests;

/// <summary>The arithmetic accepted by the rate fields in the recipe editor modal.</summary>
public class ExpressionEvaluatorTests {
	[TestCase("190", 190)]
	[TestCase("7.5", 7.5)]
	[TestCase(".5", 0.5)]
	[TestCase(" 190 ", 190)]
	[TestCase("190-30", 160)]
	[TestCase("100+50", 150)]
	[TestCase("2*7.5", 15)]
	[TestCase("480/4", 120)]
	[TestCase("1+2*3", 7, Description = "multiplication binds tighter")]
	[TestCase("10-2-3", 5, Description = "left associative")]
	[TestCase("100/4/5", 5, Description = "left associative")]
	[TestCase("-5+10", 5)]
	[TestCase("10 - -5", 15)]
	public void Evaluates(string text, double expected) {
		Assert.That(ExpressionEvaluator.TryEvaluate(text, out var value), Is.True);
		Assert.That(value, Is.EqualTo(expected).Within(1e-12));
	}

	[TestCase("")]
	[TestCase("   ")]
	[TestCase("190-", Description = "incomplete while typing")]
	[TestCase("+")]
	[TestCase("-")]
	[TestCase("1,5", Description = "only '.' decimals")]
	[TestCase("abc")]
	[TestCase("1..2")]
	[TestCase("(1+2)")]
	[TestCase("1/0", Description = "not finite")]
	[TestCase("1 2")]
	public void Rejects(string text) {
		Assert.That(ExpressionEvaluator.TryEvaluate(text, out var value), Is.False);
		Assert.That(value, Is.Zero);
	}
}
