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
        if (!request.TableScrapeFlopResult.IsPaired &&
            (request.TableScrapeFlopResult.HasBottomPair || request.TableScrapeFlopResult.HasMiddlePair ||
            request.TableScrapeFlopResult.HasTopPair || request.PlayerState.HavePocketPair ||
            request.TableScrapeFlopResult.HasOverPair))
            return true;

        return false;
    }

    private static bool HaveAce(FlopAnalyzerHelperReqest request)
    {
        if (request.TableScrapeFlopResult.HaveAce)
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
        if (request.TableScrapeFlopResult.HaveHighCards || request.TableScrapeFlopResult.HasOverCards)
            return true;
        return false;
    }

    private static bool HaveStrongHand(FlopAnalyzerHelperReqest request)
    {
        if(request.TableScrapeFlopResult.HasTwoPair || request.TableScrapeFlopResult.StrongHand())
            return true;

        return false;
    }


    #endregion
}
