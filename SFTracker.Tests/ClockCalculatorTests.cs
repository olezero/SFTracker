namespace SFTracker.Tests;

public class ClockCalculatorTests {
	[TestCase(21.5, 24)]
	[TestCase(22, 22)]
	[TestCase(5.2, 6)]
	[TestCase(13.1, 16)]
	[TestCase(7, 7)]
	[TestCase(0.5, 1)]
	[TestCase(1, 1)]
	[TestCase(2.0000000001, 2)]
	[TestCase(8.5, 12)]
	[TestCase(17, 17)]
	[TestCase(17.5, 24)]
	[TestCase(100.5, 128)]
	public void Suggested_count_is_whole_multiplier_or_next_power_of_two_times_one_or_three(double multiplier, int expected) {
		Assert.That(ClockCalculator.SuggestMachineCount(multiplier), Is.EqualTo(expected));
	}

	[TestCase(21.5, 24, 89.5834)]
	[TestCase(22, 22, 100)]
	[TestCase(2.4, 3, 80)]
	[TestCase(1, 3, 33.3334)]
	[TestCase(10, 4, 250)]
	public void Clock_is_rounded_up_to_four_decimals(double multiplier, double machines, double expected) {
		Assert.That(ClockCalculator.ClockPercent(multiplier, machines), Is.EqualTo(expected));
	}

	[Test]
	public void Machines_from_clock_inverts_the_clock() {
		Assert.That(ClockCalculator.MachinesAt(21.5, 89.5834), Is.EqualTo(24).Within(0.001));
		Assert.That(ClockCalculator.MachinesAt(10, 250), Is.EqualTo(4));
	}

	[TestCase(0.9, false)]
	[TestCase(1, true)]
	[TestCase(250, true)]
	[TestCase(250.0001, false)]
	public void Clock_range_is_1_to_250(double clock, bool valid) {
		Assert.That(ClockCalculator.IsValidClock(clock), Is.EqualTo(valid));
	}
}
