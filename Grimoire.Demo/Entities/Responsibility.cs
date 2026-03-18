namespace Grimoire.Demo.Entities;

public class Responsibility
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Title { get; set; } = "";
    public DateTime? AssignedDate { get; set; }
}
