using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Enums
{
    public static class Actions2Handed
    {
        const string ACTION_OR_4B_C = "OR / 4B / C";
        const string ACTION_OR_C_F = "OR / C / F";
        const string ACTION_3BB_C_C = "3BB / C / C";
        const string ACTION_L_C_F = "L / C / F";
        const string ACTION_L_C_C = "L / C / C";
        const string ACTION_L_F = "L / F";
        const string ACTION_CALL = "CALL";
        const string ACTION_CHECK = "CHECK";
        const string ACTION_ALL_IN = "ALL-IN";

        // OPEN RAISE
        public static List<ActionsResponse> GetOpenRaiseSuitedAction(double stack)
        {                        
            var response = new List<ActionsResponse>();

            if (stack > 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "KQ", "KJ", "QJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_C_F,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "KT", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AT", "A9", "A8", "A7" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "87", "86", "85", "84", "83", "76", "75", "74",
                        "65", "64", "54" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_C,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "K8", "Q9", "J9", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "92", "82", "73", "72", "63", "62", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 15 && stack <= 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "KQ", "KJ", "QJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_C_F,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A5", "A4", "A3", "A2", "KT", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AT", "A9", "A8", "A7", "A6" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "87", "86", "85", "84", "83", "76", "75", "74",
                        "65", "64", "54" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_C,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "Q9", "J9", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "92", "82", "73", "72", "63", "62", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 14 && stack <= 15)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "KQ", "KJ", "QJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_C_F,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KT", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AT", "A9", "A8", "A7", "A6" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A5", "A4", "A3", "A2", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "98", "97", "96", "95", "94", "87", "86", "85", "84", "76", "75", "74",
                        "65", "64", "54" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_C,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "Q9", "J9", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "T3", "T2", "93", "92", "83", "82", "73", "72", "63", "62", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 12 && stack <= 14)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "T8", "T7", "T6", "T5", "98", "97", "96", "95", "87", "86", "85", "76", "75", "74", "65", "64", "54" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_C,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "Q9", "J9", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "J3", "J2", "T4", "T3", "T2", "94", "93", "92", "84", "83", "82", "73", "72", "63", "62", "53", "52", "43", "42", "32" }
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "T8", "T7", "T6", "T5", "98", "97", "96", "95", "87", "86", "85", "76", "75", "74", "65", "64", "54", "53", "43" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_C,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "Q9", "J9", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "J3", "J2", "T4", "T3", "T2", "94", "93", "92", "84", "83", "82", "73", "72", "63", "62", "52", "42", "32" }
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "J8", "T8", "98", "87", "86", "76", "75", "65", "54" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "Q5", "J7", "J6", "J5", "T7", "T6", "T5", "97", "96", "95", "85", "74", "64", "53", "43" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "Q4", "Q3", "Q2", "J4", "J3", "J2", "T4", "T3", "T2", "94", "93", "92", "84", "83", "82", "73", "72", "63", "62", "52", "42", "32" }
                });
            }
            else if (stack > 6 && stack <= 8)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6",
                        "98", "97", "96", "87", "86", "76", "75", "65", "64", "54" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "74", "53", "43" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "Q5", "Q4", "Q3", "Q2", "J5", "J4", "J3", "J2", "T5", "T4", "T3", "T2", "95", "94", "93", "92", "85", "84", "83", "82", "73", "72", "63", "62", "52", "42", "32" }
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2",
                        "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "87", "86", "85", "84", "76", "75", "74", "65", "64", "63", "54", "53", "43" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "93", "92", "83", "82", "73", "72", "62", "52", "42", "32" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetOpenRaiseOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ", "TT", "99", "88", "77" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_C_F,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "K9", "K8", "Q9", "Q8", "J9", "J8", "T9", "T8", "98" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_C,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "K8", "Q9", "J9", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K7", "K6", "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "Q3", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "T2", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82",
                         "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 15 && stack <= 20)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ", "TT", "99", "88" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_C_F,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A5", "A4", "A3", "A2", "K9", "K8", "Q9", "Q8", "J9", "J8", "T9", "T8", "98" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_C,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "K8", "Q9", "J9", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K7", "K6", "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "Q3", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "T2", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82",
                         "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 14 && stack <= 15)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "KQ", "QQ", "JJ", "TT", "99", "88" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_C_F,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A5", "A4", "A3", "A2", "K9", "K8", "Q9", "Q8", "J9", "J8", "T9", "T8", "98" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_C,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "K8", "Q9", "J9", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K7", "K6", "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "Q3", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "T2", "97", "96", "95", "94", "87", "86", "85", "84", "76", "75", "74", "73", "65", "64", "63",
                        "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 12 && stack <= 14)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_OR_4B_C,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT", "99" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "A5", "A4", "A3", "A2", "KT", "K9", "K8", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "98" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_C,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "K8", "Q9", "J9", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K7", "K6", "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "Q3", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "97", "96", "95", "94", "87", "86", "85", "84", "76", "75", "74", "73", "65", "64", "63",
                        "54", "53", "43", "42", "32" }
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
                    Hands = new List<string> { "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT", "99" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "KT", "K9", "K8", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "98", "87", "76" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K7", "K6", "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "Q3", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "97", "96", "95", "94", "86", "85", "84", "75", "74", "73", "65", "64", "63", "54", "53", "43", "42", "32" }
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
                    Hands = new List<string> { "AA", "KK", "KQ", "KJ", "KT", "QQ", "QJ", "QT", "JJ", "JT", "TT", "99" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "K8", "Q9", "Q8", "J9", "J8", "T9", "T8", "98", "87", "76" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K7", "K6", "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "Q3", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "97", "96", "95", "94", "86", "85", "84", "75", "74", "73", "65", "64", "63", "54", "53", "43", "42", "32" }
                });
            }
            else if (stack > 6 && stack <= 8)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_C,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "87", "76" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "Q7", "Q6", "Q5", "Q4", "Q3", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "97", "96", "95", "94", "86", "85", "84", "75", "74", "73", "65", "64", "63", "54", "53", "43", "42", "32" }
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6",
                        "TT", "T9", "T8", "T7", "T6", "99", "98", "97", "96", "88", "87", "86", "77", "76", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_C_C,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_L_F,
                    Style = Styles.Pasive,
                    Position = Positions.InPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "J5", "J4", "J3", "J2", "T5", "T4", "T3", "95", "94", "85", "84", "75", "74", "73", "65", "64", "63", "54", "53", "43", "42", "32" }
                });
            }

            return response;
        }

        // vs LIMP
        public static List<ActionsResponse> GetVsLimpSuitedAction(double stack)
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
                    Hands = new List<string> { "AK", "AQ", "KQ", "KJ", "KT", "QJ", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CHECK,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92",
                        "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if ((stack > 15 && stack <= 20) || (stack > 14 && stack <= 15) || (stack > 12 && stack <= 14))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CHECK,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82",
                        "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
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
                    Hands = new List<string> { "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9" }
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
                    Action = ACTION_CHECK,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82",
                        "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CHECK,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82",
                        "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 6 && stack <= 8)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "JT", "J9", "J8", "T9", "T8", "98", "87", "76" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CHECK,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "Q5", "Q4", "Q3", "Q2", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "T2", "97", "96", "95", "94", "93", "92", "86", "85", "84", "83", "82", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "T9", "T8", "T7", "T6",
                        "98", "97", "96", "87", "86", "76", "75", "65", "64", "54" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CHECK,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "J4", "J3", "J2", "T5", "T4", "T3", "T2", "95", "94", "93", "92", "85", "84", "83", "82", "74", "73", "72", "63", "62", "53", "52", "43", "42", "32" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetVsLimpOffSuitedAction(double stack)
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
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ", "TT", "99", "88", "77" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CHECK,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92",
                        "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if ((stack > 15 && stack <= 20) || (stack > 14 && stack <= 15))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ", "TT", "99", "88", "77" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "QJ", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CHECK,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92",
                        "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 12 && stack <= 14)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "KK", "KT", "K9", "QQ", "QT", "Q9", "JJ", "JT", "J9", "TT", "T9", "99" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "QJ", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CHECK,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92",
                        "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
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
                    Hands = new List<string> { "AA", "KK", "KT", "QQ", "QT", "JJ", "JT", "TT", "99" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "QJ", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CHECK,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92",
                        "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 8 && stack <= 10)
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "QJ", "QT", "JT", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CHECK,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92",
                        "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 6 && stack <= 8)
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "QJ", "QT", "JT", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CHECK,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92",
                        "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "JT", "J9", "J8", "T9", "T8",
                        "99", "98", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CHECK,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.LimpedPot,
                    Hands = new List<string> { "Q4", "Q3", "Q2", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "T2", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54",
                        "53", "52", "43", "42", "32" }
                });
            }

            return response;
        }

        // vs OPEN RAISE
        public static List<ActionsResponse> GetVsOpenRaiseSuitedAction(double stack)
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2",
                        "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if ((stack > 15 && stack <= 20) || (stack > 14 && stack <= 15))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "QJ", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85",
                        "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 12 && stack <= 14)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "QJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85",
                        "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "QJ", "QT", "JT" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85",
                        "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
                });
            }
            else if (stack > 6 && stack <= 8)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "QJ", "QT", "Q9", "JT", "J9", "T9" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85",
                        "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32" }
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "JT", "J9", "J8", "T9", "T8", "98" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "Q4", "Q3", "Q2", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "T2", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62",
                        "54", "53", "52", "43", "42", "32" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetVsOpenRaiseOffSuitedAction(double stack)
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
                    Hands = new List<string> { "AA", "KK", "KQ", "QQ", "JJ", "TT" }
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
                    Hands = new List<string> { "A7", "A6", "A5", "A4", "A3", "A2", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6", "98", "97", "87", "76" }
                });
            }
            else if ((stack > 15 && stack <= 20) || (stack > 14 && stack <= 15))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "KJ", "KT", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A6", "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6", "98", "97", "87", "76" }
                });
            }
            else if (stack > 12 && stack <= 14)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_3BB_C_C,
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "KQ", "KJ", "KT", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6", "98", "97", "87", "76" }
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
                    Hands = new List<string> { "AA", "KK", "QQ", "JJ" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_ALL_IN,
                    Style = Styles.Agresive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "TT", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "K9", "K8", "K7", "K6", "K5", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6", "98", "97", "87", "76" }
                });
            }

            return response;
        }

        // vs ALL-IN
        public static List<ActionsResponse> GetVsAllInSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20 || (stack > 15 && stack <= 20))
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
            else if ((stack > 14 && stack <= 15) || (stack > 12 && stack <= 14))
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9" }
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "KQ", "KJ", "KT", "K9", "K8", "QJ", "QT", "Q9", "JT", "J9", "T9" }
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "QJ", "QT", "Q9", "JT", "J9", "T9" }
                });
            }
            else if (stack > 6 && stack <= 8)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "JT", "J9", "J8", "T9", "T8", "98" }
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
                    Hands = new List<string> { "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "JT", "J9", "J8", "J7", "T9", "T8", "98" }
                });
            }

            return response;
        }
        public static List<ActionsResponse> GetVsAllInOffSuitedAction(double stack)
        {
            var response = new List<ActionsResponse>();

            if (stack > 20)
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
            else if (stack > 15 && stack <= 20)
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
            else if (stack > 14 && stack <= 15)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "KK", "QQ", "QJ", "JJ", "JT", "TT", "99", "88", "77", "66", "55", "44", "33" }
                });
            }
            else if (stack > 12 && stack <= 14)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33" }
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
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "QQ", "QJ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33" }
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
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "QQ", "QJ", "JJ", "TT", "T9", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
            }
            else if (stack > 6 && stack <= 8)
            {
                response.Add(new ActionsResponse
                {
                    Action = ACTION_CALL,
                    Style = Styles.Pasive,
                    Position = Positions.OutOfPosition,
                    PreflopAction = PreflopAction.RaisedPot,
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "QQ", "QJ", "QT", "Q9", "Q8", "JJ", "JT", "J9", "TT", "T9", "99", "88", "77", "66", "55", "44", "33", "22" }
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
                    Hands = new List<string> { "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "QQ", "QJ", "QT", "Q9", "Q8", "JJ", "JT", "J9", "TT", "T9", "99", "88", "77", "66", "55", "44", "33", "22" }
                });
            }

            return response;
        }

    }
}
