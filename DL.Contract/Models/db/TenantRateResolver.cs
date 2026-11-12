using System;
using System.Collections.Generic;
using System.Linq;

namespace _0nline.Biller.DL.Contract.Models.db
{
    /// <summary>How tightly a captured rate is scoped to the invoice's client.</summary>
    public enum TenantRateScope
    {
        /// <summary>This contact at this company.</summary>
        CompanyContact = 0,

        /// <summary>Anyone at this company, when no contact-specific arrangement exists.</summary>
        Company = 1,

        /// <summary>This contact only, when no company is involved.</summary>
        Contact = 2,

        /// <summary>The tenant's standard rate, open to everyone.</summary>
        Universal = 3
    }

    /// <summary>
    /// The one place that decides which captured rates apply to an invoice and in
    /// what order. A rate is eligible only when its scope matches the invoice:
    /// a company-scoped rate is not offered on a private contact invoice, and a
    /// contact-scoped rate is not offered when a company is involved. The most
    /// specific matching rate wins; ties go to the most recently captured.
    /// </summary>
    public static class TenantRateResolver
    {
        /// <summary>
        /// Rates that may price this line, most preferred first. Archived rates are
        /// never offered. When <paramref name="rateTypeId"/> is set, only that type.
        /// </summary>
        public static IReadOnlyList<TenantRate> Ranked(
            IEnumerable<TenantRate> rates,
            int contactId,
            int? companyId,
            short? rateTypeId = null)
        {
            ArgumentNullException.ThrowIfNull(rates);

            return rates
                .Where(rate => rate.Active != false)
                .Where(rate => rateTypeId is not short type || rate.RateTypeID == type)
                .Select(rate => (Rate: rate, Scope: ScopeOf(rate, contactId, companyId)))
                .Where(match => match.Scope is not null)
                .OrderBy(match => Rank(match.Scope!.Value))
                .ThenByDescending(match => match.Rate.CapturedOn)
                .ThenByDescending(match => match.Rate.ID)
                .Select(match => match.Rate)
                .ToList();
        }

        /// <summary>The rate a line should start with, or null when none applies.</summary>
        public static TenantRate? Best(
            IEnumerable<TenantRate> rates,
            int contactId,
            int? companyId,
            short? rateTypeId = null)
            => Ranked(rates, contactId, companyId, rateTypeId).FirstOrDefault();

        /// <summary>
        /// The rate's scope for this invoice, or null when it does not apply.
        /// A company-scoped rate never applies while no company is selected, and a
        /// contact-only rate never applies while a company is selected.
        /// </summary>
        public static TenantRateScope? ScopeOf(TenantRate rate, int contactId, int? companyId)
        {
            ArgumentNullException.ThrowIfNull(rate);

            bool contactMatches = contactId > 0 && rate.ContactID == contactId;

            if (companyId is int company)
            {
                if (rate.CompanyID == company && contactMatches)
                    return TenantRateScope.CompanyContact;

                if (rate.CompanyID == company && rate.ContactID is null)
                    return TenantRateScope.Company;

                if (rate.CompanyID is null && rate.ContactID is null)
                    return TenantRateScope.Universal;

                return null;
            }

            if (rate.CompanyID is null && contactMatches)
                return TenantRateScope.Contact;

            if (rate.CompanyID is null && rate.ContactID is null)
                return TenantRateScope.Universal;

            return null;
        }

        /// <summary>Lower is more specific, so it is preferred.</summary>
        public static int Rank(TenantRateScope scope) => scope switch
        {
            TenantRateScope.CompanyContact => 0,
            TenantRateScope.Company => 1,
            TenantRateScope.Contact => 2,
            TenantRateScope.Universal => 3,
            _ => 4
        };
    }
}
