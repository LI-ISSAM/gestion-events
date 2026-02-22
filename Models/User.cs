namespace GestionEvenements.Models;

public class User
{
    public int Id { get; set; }
    public string nom { get; set; }
    public string prenom { get; set; }
    public string email { get; set; }
    public string password { get; set; }

    public List<Inscription> Inscriptions { get; set; }

}