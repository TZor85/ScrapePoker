namespace OpenScrape.App.Enums
{


    public static class Actions3Handed
    {
        const string ACTION_OR_C_C = "OR / C / C";
        const string ACTION_OR_4B_C = "OR / 4B / C";
        const string ACTION_OR_F_F = "OR / F / F";
        const string ACTION_3BB_C_C = "3BB / C / C";
        const string ACTION_L_C_F = "L / C / F";
        const string ACTION_L_C_C = "L / C / C";
        const string ACTION_L_F = "L / F";
        const string ACTION_CALL = "CALL";
        const string ACTION_ALL_IN = "ALL-IN";

        // BUTTON
        public static List<ActionsResponse> GetButtonSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if(stack > 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_F_F,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A8", "A7", "A6", "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "Q9", "Q8", "Q7", "Q6", "J9", "J8", "J7", "J6", "T9", "T8", "T7",
                        "T6", "98", "97", "96", "87", "86", "76", "75", "65" }
                });

            }
            else if (stack > 12 && stack <= 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });

                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_F_F,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "Q9", "Q8", "Q7", "J9", "J8", "J7", "T9", "T8", "T7", "98", "97", "87", "86",
                        "76", "75", "65" }
                });
            }
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ" }
                });

                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KJ", "KT", "QJ", "QT", "JT" }
                });

                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "K8", "K7", "K6", "Q9", "Q8", "Q7", "Q6", "J9", "J8", "J7", "T9", "T8", "T7", "98", "97", "87", "86", "76", "75", "65" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "QJ", "QT",
                        "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "T7", "98", "97", "87" }
                });
            }

            return response;

        }
        public static List<ActionsResponse> GetButtonOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KQ" }
                });

                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "AJ", "AT" }
                });

                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_F_F,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KJ", "KT", "K9", "K8", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "98" }
                });

                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "55", "44" }
                });
            }
            else if (stack > 12 && stack <= 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KQ", "KJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "AT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_F_F,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KT", "K9", "K8", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "55", "44" }
                });
            }
            else if (stack > 10 && stack <= 12)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "KJ", "KT", "QQ", "QJ", "QT", "JJ", "JT", "TT", "99" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "K9", "Q9", "J9", "T9" }
                });
            }
            else if (stack > 8 && stack <= 10)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "QQ", "JJ", "TT", "99" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_C,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "AA", "KK", "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "K9", "Q9", "J9", "T9" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "QQ", "QJ", "QT", "JJ", "JT", "TT",
                        "99", "88", "77", "66", "55", "44", "33", "22" }
                });
            }
                
            return response;
        }
                
        // SB vs BB
        public static List<ActionsResponse> GetSBvsBBSuitedAction(double stack) 
        {
            var response = new List<ActionsResponse>();

            if (stack > 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "KQ", "KJ", "QJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KT", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A9", "A8", "A7" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "Q9", "Q8", "Q7", "Q6", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6", "98", "97", "96", "87", "86", "76", "65", "54" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K5", "K4", "K3", "K2", "Q5", "Q4", "Q3", "Q2", "J5", "J4", "J3", "J2", "T5", "T4", "95", "94", "85", "84", "75", "74", "64" }
                });
            }
            else if (stack > 12 && stack <= 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "KQ", "KJ", "QJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KT", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A9", "A8", "A7", "A6" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "Q9", "Q8", "Q7", "Q6", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6", "98", "97", "96", "87", "86", "76", "65", "54" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K5", "K4", "K3", "K2", "Q5", "Q4", "Q3", "Q2", "J5", "J4", "J3", "J2", "T5", "T4", "95", "94", "85", "84", "75", "74", "64" }
                });

            }
            else if (stack > 10 && stack <= 12)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "98", "97", "87" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "Q7", "Q6", "Q5", "J7", "J6", "J5", "T7", "T6", "T5", "96", "95", "86", "85", "76", "75", "65" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K3", "K2", "Q4", "Q3", "Q2", "J4", "J3", "J2", "T4", "T3", "T2", "94", "84", "74", "73", "64", "63", "54", "53", "43" }
                });
            }
            else if (stack > 8 && stack <= 10)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "98", "97", "87" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "Q7", "Q6", "Q5", "J7", "J6", "J5", "T7", "T6", "T5", "96", "95", "86", "85", "76", "75", "65" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "Q4", "Q3", "Q2", "J4", "J3", "J2", "T4", "T3", "T2", "94", "84", "74", "73", "64", "63", "54", "53", "43" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2",
                        "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "98", "97", "96", "95", "87", "86", "85", "76", "75", "74", "65", "64", "54", "53" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetSBvsBBOffSuitedAction(double stack) 
        {
            var response = new List<ActionsResponse>();

            if (stack > 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66" }                
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A9", "A8", "A7", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "K9", "K8", "Q9", "Q8", "J9", "J8", "T9", "T8", "98", "87" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K7", "K6", "K5", "K4", "Q7", "Q6", "Q5", "J7", "J6", "T7", "T6", "97", "96", "86", "76", "65" }
                });
            }
            else if (stack > 12 && stack <= 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A9", "A8", "A7", "A6", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A5", "A4", "A3", "A2", "K9", "K8", "Q9", "Q8", "J9", "J8", "T9", "T8", "98", "87" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K7", "K6", "K5", "K4", "Q7", "Q6", "Q5", "J7", "J6", "T7", "T6", "97", "96", "86", "76", "65" }
                });
            }
            else if (stack > 10 && stack <= 12)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ", "TT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "Q9", "Q8", "J9", "J8", "T9", "T8", "98" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "J7", "J6", "T7", "T6", "97", "96", "87", "86", "76", "65" }
                });
            }
            else if (stack > 8 && stack <= 10)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ", "TT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "J9", "J8", "T9", "T8", "98" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "Q7", "Q6", "Q5", "Q4", "J7", "J6", "T7", "T6", "97", "96", "87", "86", "76", "65" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QQ", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4",
                        "JJ", "JT", "J9", "J8", "J7", "TT", "T9", "T8", "T7", "99", "98", "97", "88", "87", "77", "66", "55", "44", "33", "22" }
                });
            }

            return response;
        }

        // SB vs BTN LIMP
        public static List<ActionsResponse> GetSBvsBTNLimpSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2",
                        "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "87", "86", "85", "84", "76", "75", "74", "73", "65", "64", "63", "62", "54", "53", "52",
                        "43", "42", "32" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5",
                        "98", "97", "96", "95", "87", "86", "85", "84", "76", "75", "74", "65", "64", "63", "54", "53", "52", "43", "42", "32" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetSBvsBTNLimpOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "KT", "K9", "K8", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "T7", "98", "97", "87" }
                });
            }
            else if (stack > 12 && stack <= 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "KT", "K9", "K8", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "98" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "QJ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "KT", "K9", "QT", "Q9", "JT", "J9", "T9", "T8", "98", "97", "87" }
                });
            }

            return response;
        }
        
        // SB vs BTN OPEN RAISE
        public static List<ActionsResponse> GetSBvsBTNOpenRaiseSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9", "98", "87" }
                });
            }
            else if (stack > 10 && stack <= 12)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9", "98", "87", "76" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "QJ", "QT", "JT", "T9" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetSBvsBTNOpenRaiseOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A9", "A8", "A7", "A6", "A5", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
            }
            else if (stack > 10 && stack <= 12)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A8", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
            }
                
            return response;
        }

        // SB vs BTN ALL-IN
        public static List<ActionsResponse> GetSBvsBTNAllInSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "KQ", "KJ", "QJ", "QT", "JT" }
                });
            }
            else if (stack > 10 && stack <= 12)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
            }
            else if (stack > 8 && stack <= 10)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "QJ", "QT", "JT" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetSBvsBTNAllInOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
            }
            else if (stack > 10 && stack <= 12)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
            }
            else if (stack > 8 && stack <= 10)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "KK", "KQ", "KJ", "KT", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
            }

            return response;
        }

        // BB vs BTN OPEN RAISE AND SB FOLD
        public static List<ActionsResponse> GetBBvsBTNOpenRaiseAndFoldSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2",
                        "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84",
                        "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2",
                        "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84",
                        "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetBBvsBTNOpenRaiseAndFoldOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ", "TT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6", "T5",
                        "98", "97", "96", "95","87", "86", "85", "76", "75", "65", "64", "54", "43" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ", "TT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "T9",
                        "T8", "T7", "T6", "T5", "98", "97", "96", "95", "87", "86", "85", "76", "75", "74", "65", "64", "54", "43" }
                });
            }

            return response;
        }

        // BB vs BTN OPEN RAISE AND SB CALL
        public static List<ActionsResponse> GetBBvsBTNOpenRaiseAndCallSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2",
                        "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84",
                        "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 12 && stack <= 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2",
                        "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84",
                        "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 10 && stack <= 12)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2",
                        "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84",
                        "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2",
                        "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84",
                        "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetBBvsBTNOpenRaiseAndCallOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "J7", "T9", "T8", "T7", "T6", "98", "97", "96", "95", "87", "86", "85", "84", "76", "75",
                        "74", "65", "64", "63", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 10 && stack <= 12)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "T7", "98", "97", "96", "87", "86", "85", "76", "75", "74", "65", "64", "63", "54",
                        "53", "52", "43", "42", "32" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "T7", "98", "97", "96", "87", "86", "85", "76", "75", "74", "65", "64", "63", "54",
                        "53", "52", "43", "42", "32" }
                });
            }

            return response;
        }

        // BB vs BTN OPEN RAISE AND SB 3BET
        public static List<ActionsResponse> GetBBvsBTNOpenRaiseAnd3BetSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A9", "A8", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KQ", "KJ", "QJ", "QT", "JT" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetBBvsBTNOpenRaiseAnd3BetOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AT", "KK", "KQ", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
            }
            else if (stack > 10 && stack <= 12)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "QQ", "JJ", "TT", "99", "88", "77" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "KQ" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "QQ", "JJ", "TT", "99" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "KQ", "KJ", "QJ" }
                });
            }

            return response;
        }

        // BB vs BTN OPEN RAISE AND SB ALL-IN
        public static List<ActionsResponse> GetBBvsBTNOpenRaiseAndAllInSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "KQ" }
                });
            }
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4",
                        "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "98" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetBBvsBTNOpenRaiseAndAllInOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55" }
                });
            }
            else if (stack > 10 && stack <= 12)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44" }
                });
            }
            else if (stack > 8 && stack <= 10)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT", "99", "88", "77", "66", "55", "44" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QQ", "QJ", "QT", "Q9", "Q8", "Q7",
                        "JJ", "JT", "J9", "J8", "TT", "T9", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
            }

            return response;
        }

        // BB vs BTN LIMP AND SB FOLD
        public static List<ActionsResponse> GetBBvsBTNLimpAndFoldSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53",
                        "52", "43", "42", "32" }
                });
            }
            else if (stack > 12 && stack <= 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "K9", "QJ", "QT", "Q9", "JT", "J9", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94",
                        "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetBBvsBTNLimpAndFoldOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A4", "A3", "A2", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 12 && stack <= 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "KT", "QQ", "QJ", "QT", "JJ", "JT", "TT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96",
                        "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }

            return response;
        }

        // BB vs BTN LIMP AND SB CALL
        public static List<ActionsResponse> GetBBvsBTNLimpAndCallSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2",
                        "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2",
                        "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetBBvsBTNLimpAndCallOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A4", "A3", "A2", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 12 && stack <= 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "QJ", "JJ", "TT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2",
                        "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }

            return response;
        }

        // BB vs BTN LIMP AND SB 3BET
        public static List<ActionsResponse> GetBBvsBTNLimpAnd3BetSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "QJ", "QT", "Q9", "Q8", "Q7", "JT", "J9", "J8", "J7", "T9", "T8", "T7", "98", "97", "87", "86", "76", "75", "65" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "KQ", "KJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KT", "K9", "K8", "K7", "QJ", "QT", "Q9", "Q8", "Q7", "JT", "J9", "J8", "J7", "T9", "T8", "T7", "98", "97", "87" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetBBvsBTNLimpAnd3BetOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "JJ", "TT", "99", "88", "77", "66", "55" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A9", "A8", "A7", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9", "T8", "98", "44", "33", "22" }
                });
            }
            else if (stack > 10 && stack <= 12)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "KQ", "JJ", "TT", "99", "88" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A8", "A7", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9", "77", "66", "55", "44", "33", "22" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "KQ", "JJ", "TT", "99" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "A8", "A7", "KK", "KJ", "KT", "K9", "QQ", "QJ", "QT", "Q9", "JT", "J9", "T9", "77", "66", "55", "44", "33", "22" }
                });
            }

            return response;
        }

        // BB vs BTN LIMP AND SB ALL-IN
        public static List<ActionsResponse> GetBBvsBTNLimpAndAllInSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "KQ", "QJ", "JT" }
                });
            }
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4",
                        "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "98" }
                });
            }

            return response;

        }
        public static List<ActionsResponse> GetBBvsBTNLimpAndAllInOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44" }
                });
            }
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QQ", "QJ", "QT", "Q9", "Q8", "Q7",
                        "JJ", "JT", "J9", "J8", "TT", "T9", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
            }

            return response;
        }

        // BB vs BTN ALL-IN AND SB FOLD
        public static List<ActionsResponse> GetBBvsBTNAllInAndFoldSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
            }
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "KJ", "KT", "QJ", "QT", "JT", "J9", "T9", "98" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetBBvsBTNAllInAndFoldOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55" }
                });
            }
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KK", "KQ", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "KK", "KQ", "KJ", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
            }

            return response;
        }

        // BB vs BTN ALL-IN AND SB CALL
        public static List<ActionsResponse> GetBBvsBTNAllInAndCallSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
            }
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9", "98" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "KJ", "KT", "QJ", "QT", "JT", "J9", "T9", "98" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetBBvsBTNAllInAndCallOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55" }
                });
            }
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT", "99", "88", "77", "66", "55", "44" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "KK", "KQ", "KJ", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44" }
                });
            }

            return response;
        }

        // BB vs SB OPEN RAISE
        public static List<ActionsResponse> GetBBvsSBOpenRaiseSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A7", "A6", "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if ((stack > 12 && stack <= 20) || (stack > 10 && stack <= 12))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A8", "A7", "A6", "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9",
                        "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetBBvsSBOpenRaiseOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "KK", "KQ", "QQ", "JJ", "TT", "99", "88" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "77", "66" }
                });                
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6",
                        "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "87", "86", "85", "84", "76", "75", "74", "73", "65", "64", "63", "62", "55", "54", "53", "52",
                        "44", "43", "42", "33", "32", "22" }
                });
            }
            else if ((stack > 12 && stack <= 20) || (stack > 10 && stack <= 12))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "KK", "KQ", "QQ", "JJ", "TT", "99" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4",
                        "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "87", "86", "85", "84", "76", "75", "74", "73", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ", "TT", "99" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4",
                        "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "87", "86", "85", "76", "75", "74", "65", "64", "54", "43" }
                });
            }

            return response;
            
        }

        // BB vs SB LIMP
        public static List<ActionsResponse> GetBBvsSBLimpSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20) || (stack > 10 && stack <= 12))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "KJ", "KT", "K9", "K8", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "98", "97", "96", "87", "86", "76", "75", "65" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "K7", "K6", "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "T2", "95", "94", "93", "92", "85",
                        "84", "83", "82", "74", "73", "72", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2",
                        "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetBBvsSBLimpOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20) || (stack > 10 && stack <= 12))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KK", "KQ", "KJ", "KT", "K9", "QQ", "QJ", "QT", "Q9", "JJ", "JT", "J9", "TT", "T9", "99", "88" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2",
                        "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "55", "54", "53", "52", "44", "43", "42", "33", "32", "22" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "KT", "QQ", "QJ", "QT", "JJ", "JT", "TT", "99" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "KQ", "KJ", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A7", "A6", "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4",
                        "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "55", "54", "53", "52", "44", "43", "42", "33", "32", "22" }
                });
            }

            return response;

        }

        // BB vs SB ALL-IN
        public static List<ActionsResponse> GetBBvsSBAllInSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "KQ", "KJ", "QJ", "QT", "JT" }
                });
            }
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "QJ", "QT", "Q9", "JT", "J9", "T9" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2",
                        "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "98" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetBBvsSBAllInOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 12 && stack <= 20))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44" }
                });
            }
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "QQ", "QJ", "QT", "JJ", "TT", "99", "88", "77", "66", "55", "44" }
                });
            }
            else
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QQ", "QJ", "QT", "Q9", "Q8", "Q7", "JJ",
                        "JT", "J9", "J8", "TT", "T9", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
            }

            return response;
        }

    }
}