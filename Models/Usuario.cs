namespace Soen___Torrezim.Models
{
    /// <summary>Usuário do sistema (login).</summary>
    public class Usuario
    {
        public long Id { get; set; }
        public string Login { get; set; }
        public string Senha { get; set; }   // hash SHA-256
        public string Nome { get; set; }
        public string Perfil { get; set; }  // admin | operador
        public bool Ativo { get; set; } = true;
    }
}
