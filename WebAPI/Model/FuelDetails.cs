using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebAPI.Model
{
    /// <summary>
    /// A fuel fill-up. The validation attributes apply to entries posted to the API.
    /// </summary>
    public class FuelDetail : IValidatableObject
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "MeterReading must be a positive whole number.")]
        public int MeterReading { get; set; }
        [Range(0.01, double.MaxValue, ErrorMessage = "TotalPrice must be greater than 0.")]
        public double TotalPrice { get; set; }
        [Range(0.01, double.MaxValue, ErrorMessage = "AddedFuel must be greater than 0.")]
        public double AddedFuel { get; set; }
        [StringLength(1000, ErrorMessage = "Note must be at most 1000 characters.")]
        public string Note { get; set; }

        /// <summary>When the fill-up happened (UTC), as chosen by the user.</summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>When the entry was last stored (UTC); set by the server.</summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// Checks the fill-up date, which attributes can't express: it must be set and not in the future.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CreatedAt == default(DateTime))
            {
                yield return new ValidationResult("CreatedAt is required.", new[] { "CreatedAt" });
            }
            // A day of slack so that "today" picked in a time zone ahead of the server's is not rejected.
            else if (CreatedAt.ToUniversalTime() > DateTime.UtcNow.AddDays(1))
            {
                yield return new ValidationResult("CreatedAt cannot be in the future.", new[] { "CreatedAt" });
            }
        }
    }
}