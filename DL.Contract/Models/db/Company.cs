using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using _0nline.Biller.DL.Contract.Enums;

namespace _0nline.Biller.DL.Contract.Models.db
{
    public partial class Company : IValidatableObject
    {
        public string? PhoneNo { 
            get { return Phone?.ToString("+0"); } 
            set { Phone = string.IsNullOrEmpty(value) ? null : long.Parse(value); } 
        }

        /// <summary>
        /// Detailed read model: the contacts linked to this company. Populated by
        /// <c>CompanyService</c> when a single company is loaded; not a database column,
        /// so it is excluded from insert and update field lists.
        /// </summary>
        [NotMapped]
        [Editable(false)]
        public List<Contact> Contacts { get; set; } = new();

        private bool _taxRegistered;

        /// <summary>
        /// The switch on the company form. A company is tax-registered when it names
        /// a scheme; there is no separate column to drift from it. The backing field
        /// lets the switch stay on while the user is still choosing a scheme, which is
        /// what makes the required-field error possible before a scheme exists.
        /// </summary>
        [NotMapped]
        [Editable(false)]
        [JsonIgnore]
        public bool TaxRegistered
        {
            get => _taxRegistered || TaxSchemeID.HasValue;
            set
            {
                _taxRegistered = value;

                if (value) return;

                TaxSchemeID = null;
                TaxNumber = null;
            }
        }

        /// <summary>
        /// The scheme the picker sees. The column is nullable, so this is mostly a
        /// typed view: it keeps the enum out of the API payload and lets the form bind
        /// without a sentinel 0.
        /// </summary>
        [Editable(false)]
        [JsonIgnore]
        public _0nline.Biller.DL.Contract.Enums.TaxScheme? TaxSchemeSelection
        {
            get => TaxSchemeID is short id
                ? (_0nline.Biller.DL.Contract.Enums.TaxScheme)id
                : null;
            set => TaxSchemeID = value is null ? null : (short)value;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!TaxRegistered)
                yield break;

            if (TaxSchemeID is not short id)
            {
                yield return new ValidationResult(
                    "Choose a tax scheme.", new[] { nameof(TaxSchemeSelection) });
            }
            else if (!Enum.IsDefined((_0nline.Biller.DL.Contract.Enums.TaxScheme)id))
            {
                yield return new ValidationResult(
                    "Choose a valid tax scheme.", new[] { nameof(TaxSchemeSelection) });
            }

            if (string.IsNullOrWhiteSpace(CountryCode))
            {
                yield return new ValidationResult(
                    "Choose the country where the business is registered for tax.",
                    new[] { nameof(CountryCode) });
            }

            if (string.IsNullOrWhiteSpace(TaxNumber))
            {
                yield return new ValidationResult(
                    "Enter the tax registration number, or switch tax registration off.",
                    new[] { nameof(TaxNumber) });
            }
        }
    }
}
