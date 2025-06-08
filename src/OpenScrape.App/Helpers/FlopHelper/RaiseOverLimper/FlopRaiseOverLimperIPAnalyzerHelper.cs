namespace OpenScrape.App.Helpers.FlopHelper.RaiseOverLimper;

public static class FlopRaiseOverLimperIPAnalyzerHelper
{
    #region [1/3 Bote]

    public static bool IsActionBetOneThird(FlopAnalyzerHelperReqest request)
    {
        if (HaveAnyPair(request) || HaveAce(request))
            return true;

        return false;
    }

    private static bool HaveAnyPair(FlopAnalyzerHelperReqest request)
    {
        if (!request.TableScrapeFlopResult.BoardTexture.IsPaired &&
            (request.TableScrapeFlopResult.HeroStrength.HasBottomPair || request.TableScrapeFlopResult.HeroStrength.HasMiddlePair ||
            request.TableScrapeFlopResult.HeroStrength.HasTopPair || request.PlayerState.HavePocketPair ||
            request.TableScrapeFlopResult.HeroStrength.HasOverPair))
            return true;

        return false;
    }

    private static bool HaveAce(FlopAnalyzerHelperReqest request)
    {
        if (request.TableScrapeFlopResult.BoardTexture.HasAce)
            return true;

        return false;
    }

    #endregion

    #region [1/2 Bote]

    public static bool IsActionBetHalf(FlopAnalyzerHelperReqest request)
    {
        if (HaveOvercards(request) || HaveStrongHand(request))
            return true;
        return false;
    }
    
    private static bool HaveOvercards(FlopAnalyzerHelperReqest request)
    {
        if (request.TableScrapeFlopResult.HeroStrength.HasHighCard || request.TableScrapeFlopResult.HeroStrength.HasOverCards)
            return true;
        return false;
    }

    private static bool HaveStrongHand(FlopAnalyzerHelperReqest request)
    {
        if(request.TableScrapeFlopResult.HeroStrength.HasTwoPair || request.TableScrapeFlopResult.HasStrongHand)
            return true;

        return false;
    }


    #endregion
}
