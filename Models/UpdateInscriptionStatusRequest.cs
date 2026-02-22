namespace GestionEvenements.Models
{
    public class UpdateInscriptionStatusRequest
    {
        public int InscriptionId { get; set; }
        public string NewStatus { get; set; }
    }
}
