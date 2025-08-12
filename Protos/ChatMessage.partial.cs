using System.Globalization;
using GrpcService.Protos;

namespace GrpcService.Protos;

// This partial class adds a Decimal-friendly API over the string-based
// precise_fraction field in ChatMessage. It assumes US culture formatting
// and is intended for values in [0, 1].
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

            if (value < 0m || value > 1m)
            {
                throw new ArgumentOutOfRangeException(nameof(PreciseFractionDecimal), "Value must be between 0 and 1 inclusive.");
            }

            // Use "G29" to preserve as many significant digits as possible without scientific notation
            PreciseFraction = value.Value.ToString("G29", UsCulture);
        }
    }
}

