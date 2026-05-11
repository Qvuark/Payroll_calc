namespace PayrollCalc.Core.Entities;

public class WorkCalendar
{
    public int Id { get; set; }
    public int Year { get; set; } = 0;
    public int Month { get; set; } = 0;
    public int WorkDays { get; set; } = 0;
}