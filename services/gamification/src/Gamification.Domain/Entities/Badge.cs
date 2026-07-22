namespace Gamification.Domain.Entities;

/// <summary>Rozet katalogu — seed ile dolar, runtime'da degismez.</summary>
public class Badge
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
}
