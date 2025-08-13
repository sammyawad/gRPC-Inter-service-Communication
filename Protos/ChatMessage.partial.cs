using System.Globalization;
using GrpcService.Protos;

namespace GrpcService.Protos;

// This partial class adds a Decimal-friendly API over the string-based
// precise_fraction field in ChatMessage. It assumes US culture formatting
// and supports arbitrary numeric ranges.
public partial class ChatMessage
{
    private static readonly CultureInfo UsCulture = CultureInfo.GetCultureInfo("en-US");

    public decimal? PreciseFractionDecimal
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PreciseFraction)) return null;
            if (decimal.TryParse(PreciseFraction, NumberStyles.Number, UsCulture, out var value))
            {
                return value;
            }
            return null;
        }
        set
        {
            if (value is null)
            {
                PreciseFraction = string.Empty;
                return;
            }

            // Accept any decimal range; the frontend will scale axes dynamically.

            // Use "G29" to preserve as many significant digits as possible without scientific notation
            PreciseFraction = value.Value.ToString("G29", UsCulture);
        }
    }
}

