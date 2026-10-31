using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace _0nline.Biller.DL.Contract.Models.db
{
    /// <summary>
    /// Hand-written rules for a rate. The generated model cannot carry them, and its
    /// non-nullable <see cref="RateTypeID"/> cannot represent "nothing selected" (the
    /// sentinel is 0). These run only where a <c>DataAnnotationsValidator</c> validates
    /// the instance; the API does not validate its input.
    /// </summary>
    public partial class TenantRate : IValidatableObject
    {
        /// <summary>
        /// The rate type as the picker sees it: <c>null</c> means "not chosen".
        /// </summary>
        /// <remarks>
        /// The column is NOT NULL, so the entity carries 0 before a type is chosen, which
        /// a <c>MudSelect</c> renders as the literal "0". Binding through this nullable
        /// view gives the picker an empty initial state and lets <see cref="RequiredAttribute"/>
        /// fire on blur and on submit; the getter/setter keep <see cref="RateTypeID"/> in sync.
        /// It is not a column: <see cref="JsonIgnoreAttribute"/> keeps it out of the API payload
        /// and <see cref="EditableAttribute"/> keeps DbService from writing it.
        /// </remarks>
        [Required(ErrorMessage = "Choose a rate type.")]
        [Editable(false)]
        [JsonIgnore]
        public short? RateTypeSelection
        {
            get => RateTypeID == 0 ? null : RateTypeID;
            set => RateTypeID = value ?? 0;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DefaultRate is < 0)
            {
                yield return new ValidationResult(
                    "The rate cannot be negative.", new[] { nameof(DefaultRate) });
            }

            if (StartDate is DateTime start && EndDate is DateTime end && end.Date < start.Date)
            {
                yield return new ValidationResult(
                    "The end date must be on or after the start date.", new[] { nameof(EndDate) });
            }
        }
    }
}
