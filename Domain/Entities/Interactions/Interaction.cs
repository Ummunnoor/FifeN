using System;
using Domain.Entities.Enums;

namespace Domain.Entities.Interactions
{
    /// <summary>
    /// A logged "Contact / I'm interested" event by a signed-in buyer — recorded on-platform
    /// <em>before</em> the WhatsApp hand-off. It is the closest observable signal to a sale and powers
    /// the vendor leads dashboard, cross-discovery metrics, and review eligibility.
    /// </summary>
    public class Interaction
    {
        public Guid Id { get; set; }

        /// <summary>The buyer who expressed interest.</summary>
        public Guid BuyerUserId { get; set; }

        /// <summary>The vendor contacted.</summary>
        public Guid VendorProfileId { get; set; }

        /// <summary>The listing the interest was about.</summary>
        public Guid ProductId { get; set; }

        /// <summary>Optional buyer note shown to the vendor as part of the lead.</summary>
        public string? BuyerMessage { get; set; }

        /// <summary>Optional offer price the buyer proposed.</summary>
        public decimal? OfferPrice { get; set; }

        /// <summary>How far the vendor has progressed the lead.</summary>
        public LeadStatus LeadStatus { get; set; } = LeadStatus.New;

        /// <summary>
        /// True when the buyer had no prior interaction with this vendor — the north-star
        /// "cross-discovery" signal that a true marketplace is forming.
        /// </summary>
        public bool IsCrossDiscovery { get; set; }

        /// <summary>When the interaction occurred (UTC).</summary>
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
