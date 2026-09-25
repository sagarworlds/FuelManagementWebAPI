using System;

namespace WebAPI.Model
{
    /// <summary>
    /// A user as returned by the API. It deliberately has no password field.
    /// </summary>
    public class UserResponse
    {
        public int Id { get; set; }

        public string Email { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// Copies the public fields of a stored user.
        /// </summary>
        /// <exception cref="ArgumentNullException">When <paramref name="user"/> is null.</exception>
        public static UserResponse From(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            return new UserResponse { Id = user.Id, Email = user.Email, CreatedAt = user.CreatedAt, ModifiedAt = user.ModifiedAt };
        }
    }
}
