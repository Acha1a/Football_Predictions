namespace FootballPredictor.Web.Models;

public class PredictionFactor
{
    public int Id { get; set; }
    public int PredictionId { get; set; }
    public Prediction? Prediction { get; set; }

    public string Name { get; set; } = "";
    public decimal Value { get; set; }   // нормализованное значение 0..1
    public decimal Weight { get; set; }  // вес фактора
    public decimal Contribution { get; set; } // Value * Weight
    public string Explanation { get; set; } = "";
}