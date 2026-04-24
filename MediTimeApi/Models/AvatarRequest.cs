namespace MediTimeApi.Models
{
    /// <summary>
    /// DTO para el endpoint PUT /Usuarios/{id}/avatar.
    /// </summary>
    public class AvatarRequest
    {
        public string AvatarBase64 { get; set; } = string.Empty;
    }
}
