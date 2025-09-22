using OpenScrape.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Models;

/// <summary>
/// Resultado interno de evaluación
/// </summary>
internal sealed record BestHandResult(
    HandRank Ranking,
    List<NormalizedCard> Cards,
    List<NormalizedCard> Kickers
);
