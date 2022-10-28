namespace OpenScrape.App.Enums
{
    public static class Actions3Handed
    {
        // BUTTON
        public static List<KeyValuePair<string, List<string>>> GetButtonSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / C / C", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("OR / F / F", new List<string>
                    {
                        "A8", "A7", "A6", "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "Q9", "Q8", "Q7", "Q6", "J9", "J8", "J7", "J6", "T9", "T8", "T7",
                        "T6", "98", "97", "96", "87", "86", "76", "75", "65"
                    })
                };
            else if (stack > 12 && stack <= 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / C / C", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("OR / F / F", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "Q9", "Q8", "Q7", "J9", "J8", "J7", "T9", "T8", "T7", "98", "97", "87", "86",
                        "76", "75", "65"
                    })
                };
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ"
                    }),
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "K9", "K8", "K7", "K6", "Q9", "Q8", "Q7", "Q6", "J9", "J8", "J7", "T9", "T8", "T7", "98", "97", "87", "86", "76", "75", "65"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "QJ", "QT",
                        "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "T7", "98", "97", "87"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetButtonOffSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / C / C", new List<string>
                    {
                        "KQ"
                    }),
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "AJ", "AT"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "55", "44"
                    }),
                    new KeyValuePair<string, List<string>>("OR / F / F", new List<string>
                    {
                        "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KJ", "KT", "K9", "K8", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "98"
                    })
                };
            else if (stack > 12 && stack <= 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / C / C", new List<string>
                    {
                        "KQ", "KJ"
                    }),
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "AT"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "55", "44"
                    }),
                    new KeyValuePair<string, List<string>>("OR / F / F", new List<string>
                    {
                        "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KT", "K9", "K8", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AA", "KK", "KJ", "KT", "QQ", "QJ", "QT", "JJ", "JT", "TT", "99"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "K9", "Q9", "J9", "T9"
                    })
                };
            else if (stack > 8 && stack <= 10)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "QQ", "JJ", "TT", "99"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / C", new List<string>
                    {
                        "AA", "KK", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "K9", "Q9", "J9", "T9"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "QQ", "QJ", "QT", "JJ", "JT", "TT",
                        "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
        }

        // SB vs BB
        public static List<KeyValuePair<string, List<string>>> GetSBvsBBSuitedAction(double stack) 
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "KQ", "KJ", "QJ"
                    }),
                    new KeyValuePair<string, List<string>>("OR / C / C", new List<string>
                    {
                        "KT", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "Q9", "Q8", "Q7", "Q6", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6", "98", "97", "96", "87", "86", "76", "65", "54"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "K5", "K4", "K3", "K2", "Q5", "Q4", "Q3", "Q2", "J5", "J4", "J3", "J2", "T5", "T4", "95", "94", "85", "84", "75", "74", "64"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "A9", "A8", "A7"
                    })
                };
            else if (stack > 12 && stack <= 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "KQ", "KJ", "QJ"
                    }),
                    new KeyValuePair<string, List<string>>("OR / C / C", new List<string>
                    {
                        "KT", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "Q9", "Q8", "Q7", "Q6", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6", "98", "97", "96", "87", "86", "76", "65", "54"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "K5", "K4", "K3", "K2", "Q5", "Q4", "Q3", "Q2", "J5", "J4", "J3", "J2", "T5", "T4", "95", "94", "85", "84", "75", "74", "64"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "A9", "A8", "A7", "A6"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                       "Q7", "Q6", "Q5", "J7", "J6", "J5", "T7", "T6", "T5", "96", "95", "86", "85", "76", "75", "65"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "K3", "K2", "Q4", "Q3", "Q2", "J4", "J3", "J2", "T4", "T3", "T2", "94", "84", "74", "73", "64", "63", "54", "53", "43"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "98", "97", "87"
                    })
                };
            else if (stack > 8 && stack <= 10)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                       "Q7", "Q6", "Q5", "J7", "J6", "J5", "T7", "T6", "T5", "96", "95", "86", "85", "76", "75", "65"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "Q4", "Q3", "Q2", "J4", "J3", "J2", "T4", "T3", "T2", "94", "84", "74", "73", "64", "63", "54", "53", "43"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "98", "97", "87"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", 
                        "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "98", "97", "96", "95", "87", "86", "85", "76", "75", "74", "65", "64", "54", "53"
                    })
                };

        }
        public static List<KeyValuePair<string, List<string>>> GetSBvsBBOffSuitedAction(double stack) 
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66"
                    }),
                    new KeyValuePair<string, List<string>>("OR / C / C", new List<string>
                    {
                        "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "K9", "K8", "Q9", "Q8", "J9", "J8", "T9", "T8", "98", "87"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "K7", "K6", "K5", "K4", "Q7", "Q6", "Q5", "J7", "J6", "T7", "T6", "97", "96", "86", "76", "65"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "A9", "A8", "A7", "55", "44", "33", "22"
                    })
                };
            else if (stack > 12 && stack <= 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66"
                    }),
                    new KeyValuePair<string, List<string>>("OR / C / C", new List<string>
                    {
                        "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "A5", "A4", "A3", "A2", "K9", "K8", "Q9", "Q8", "J9", "J8", "T9", "T8", "98", "87"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "K7", "K6", "K5", "K4", "Q7", "Q6", "Q5", "J7", "J6", "T7", "T6", "97", "96", "86", "76", "65"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "A9", "A8", "A7", "A6", "55", "44", "33", "22"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ", "TT"
                    }),
                    new KeyValuePair<string, List<string>>("OR / C / C", new List<string>
                    {
                        "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                       "Q9", "Q8", "J9", "J8", "T9", "T8", "98"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "J7", "J6", "T7", "T6", "97", "96", "87", "86", "76", "65"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
            else if (stack > 8 && stack <= 10)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ", "TT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                       "J9", "J8", "T9", "T8", "98"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "Q7", "Q6", "Q5", "Q4", "J7", "J6", "T7", "T6", "97", "96", "87", "86", "76", "65"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QQ", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", 
                        "JJ", "JT", "J9", "J8", "J7", "TT", "T9", "T8", "T7", "99", "98", "97", "88", "87", "77", "66", "55", "44", "33", "22"
                    })
                };


        }

        // SB vs BTN LIMP
        public static List<KeyValuePair<string, List<string>>> GetSBvsBTNLimpSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2",
                        "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "87", "86", "85", "84", "76", "75", "74", "73", "65", "64", "63", "62", "54", "53", "52",
                        "43", "42", "32"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5",
                        "98", "97", "96", "95", "87", "86", "85", "84", "76", "75", "74", "65", "64", "63", "54", "53", "52", "43", "42", "32"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetSBvsBTNLimpOffSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "KT", "K9", "K8", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "T7", "98", "97", "87"
                    })
                };
            else if (stack > 12 && stack <= 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "KT", "K9", "K8", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "98"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "QJ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                         "KT", "K9", "QT", "Q9", "JT", "J9", "T9", "T8", "98", "97", "87"
                    })
                };
        }
        
        // SB vs BTN OPEN RAISE
        public static List<KeyValuePair<string, List<string>>> GetSBvsBTNOpenRaiseSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9", "98", "87"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9", "98", "87", "76"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "QJ", "QT", "JT", "T9"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetSBvsBTNOpenRaiseOffSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A9", "A8", "A7", "A6", "A5", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A8", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    })
                };
        }

        // SB vs BTN ALL-IN
        public static List<KeyValuePair<string, List<string>>> GetSBvsBTNAllInSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "KQ", "KJ", "QJ", "QT", "JT"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    })
                };
            else if (stack > 8 && stack <= 10)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "QJ", "QT", "JT"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetSBvsBTNAllInOffSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
            else if (stack > 8 && stack <= 10)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "KK", "KQ", "KJ", "KT", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
        }

        // BB vs BTN OPEN RAISE AND SB FOLD
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNOpenRaiseAndFoldSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2",
                        "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84",
                        "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2",
                        "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84",
                        "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNOpenRaiseAndFoldOffSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ", "TT"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6", "T5",
                        "98", "97", "96", "95","87", "86", "85", "76", "75", "65", "64", "54", "43"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ", "TT"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "T9",
                        "T8", "T7", "T6", "T5", "98", "97", "96", "95", "87", "86", "85", "76", "75", "74", "65", "64", "54", "43"
                    })
                };
        }

        // BB vs BTN OPEN RAISE AND SB CALL
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNOpenRaiseAndCallSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8" 
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2",
                        "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84",
                        "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if (stack > 12 && stack <= 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2",
                        "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84",
                        "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2",
                        "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84",
                        "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2",
                        "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84",
                        "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNOpenRaiseAndCallOffSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "J7", "T9", "T8", "T7", "T6", "98", "97", "96", "95", "87", "86", "85", "84", "76", "75",
                        "74", "65", "64", "63", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "T7", "98", "97", "96", "87", "86", "85", "76", "75", "74", "65", "64", "63", "54",
                        "53", "52", "43", "42", "32"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "T7", "98", "97", "96", "87", "86", "85", "76", "75", "74", "65", "64", "63", "54",
                        "53", "52", "43", "42", "32"
                    })
                };
        }

        // BB vs BTN OPEN RAISE AND SB 3BET
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNOpenRaiseAnd3BetSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A9", "A8", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "KQ", "KJ","QJ", "QT", "JT"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNOpenRaiseAnd3BetOffSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AT", "KK", "KQ", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "QQ", "JJ", "TT", "99", "88", "77"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "KK", "KQ"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "QQ", "JJ", "TT", "99"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "KK", "KQ", "KJ", "QJ"
                    })
                };
        }

        // BB vs BTN OPEN RAISE AND SB ALL-IN
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNOpenRaiseAndAllInSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "KQ"
                    })
                };
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4",
                        "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "98"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNOpenRaiseAndAllInOffSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44"
                    })
                };
            else if (stack > 8 && stack <= 10)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT", "99", "88", "77", "66", "55", "44"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QQ", "QJ", "QT", "Q9", "Q8", "Q7",
                        "JJ", "JT", "J9", "J8", "TT", "T9", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
        }

        // BB vs BTN LIMP AND SB FOLD
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNLimpAndFoldSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53",
                        "52", "43", "42", "32"
                    })
                };
            else if (stack > 12 && stack <= 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6" 
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "K9", "QJ", "QT", "Q9", "JT", "J9", "T9"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94",
                        "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNLimpAndFoldOffSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A4", "A3", "A2", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if (stack > 12 && stack <= 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "KT", "QQ", "QJ", "QT", "JJ", "JT", "TT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96",
                        "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
        }

        // BB vs BTN LIMP AND SB CALL
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNLimpAndCallSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2",
                        "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                         "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2",
                        "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNLimpAndCallOffSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A4", "A3", "A2", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if (stack > 12 && stack <= 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ", "QJ", "JJ", "TT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2",
                        "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
        }

        // BB vs BTN LIMP AND SB 3BET
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNLimpAnd3BetSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "QJ", "QT", "Q9", "Q8", "Q7", "JT", "J9", "J8", "J7", "T9", "T8", "T7", "98", "97", "87", "86", "76", "75", "65"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "KQ", "KJ"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KT", "K9", "K8", "K7", "QJ", "QT", "Q9", "Q8", "Q7", "JT", "J9", "J8", "J7", "T9", "T8", "T7", "98", "97", "87"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNLimpAnd3BetOffSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "JJ", "TT", "99", "88", "77", "66", "55"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A9", "A8", "A7", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9", "T8", "98", "44", "33", "22"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "KQ", "JJ", "TT", "99", "88"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A8", "A7", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9", "77", "66", "55", "44", "33", "22"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "KQ", "JJ", "TT", "99"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "A8", "A7", "KK", "KJ", "KT", "K9", "QQ", "QJ", "QT", "Q9", "JT", "J9", "T9", "77", "66", "55", "44", "33", "22"
                    })
                };
        }

        // BB vs BTN LIMP AND SB ALL-IN
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNLimpAndAllInSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "KQ", "QJ", "JT"
                    })
                };
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4",
                        "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "98"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNLimpAndAllInOffSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44"
                    })
                };
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QQ", "QJ", "QT", "Q9", "Q8", "Q7",
                        "JJ", "JT", "J9", "J8", "TT", "T9", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
        }

        // BB vs BTN ALL-IN AND SB FOLD
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNAllInAndFoldSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    })
                };
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "KJ", "KT", "QJ", "QT", "JT", "J9", "T9", "98"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNAllInAndFoldOffSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55"
                    })
                };
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KK", "KQ", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "KK", "KQ", "KJ", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
        }

        // BB vs BTN ALL-IN AND SB CALL
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNAllInAndCallSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    })
                };
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9", "98"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "KJ", "KT", "QJ", "QT", "JT", "J9", "T9", "98"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetBBvsBTNAllInAndCallOffSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55"
                    })
                };
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT", "99", "88", "77", "66", "55", "44"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "KK", "KQ", "KJ", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44"
                    })
                };
        }

        // BB vs SB OPEN RAISE
        public static List<KeyValuePair<string, List<string>>> GetBBvsSBOpenRaiseSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A7", "A6", "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if ((stack > 12 && stack <= 20) || (stack > 10 && stack <= 12))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A8", "A7", "A6", "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6",
                        "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9",
                        "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetBBvsSBOpenRaiseOffSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "77", "66"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "KK", "KQ", "QQ", "JJ", "TT", "99", "88"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6",
                        "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "87", "86", "85", "84", "76", "75", "74", "73", "65", "64", "63", "62", "55", "54", "53", "52",
                        "44", "43", "42", "33", "32", "22"
                    })
                };
            else if ((stack > 12 && stack <= 20) || (stack > 10 && stack <= 12))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "KK", "KQ", "QQ", "JJ", "TT", "99"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4",
                        "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "87", "86", "85", "84", "76", "75", "74", "73", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ", "TT", "99"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4",
                        "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "87", "86", "85", "76", "75", "74", "65", "64", "54", "43"
                    })
                };
            
        }

        // BB vs SB LIMP
        public static List<KeyValuePair<string, List<string>>> GetBBvsSBLimpSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20) || (stack > 10 && stack <= 12))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "KJ", "KT", "K9", "K8", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "98", "97", "96", "87", "86", "76", "75", "65"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "K7", "K6", "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "T2", "95", "94", "93", "92", "85",
                        "84", "83", "82", "74", "73", "72", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2",
                        "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetBBvsSBLimpOffSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20) || (stack > 10 && stack <= 12))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KK", "KQ", "KJ", "KT", "K9", "QQ", "QJ", "QT", "Q9", "JJ", "JT", "J9", "TT", "T9", "99", "88"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2",
                        "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "55", "54", "53", "52", "44", "43", "42", "33", "32", "22"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "KQ", "KJ", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "KT", "QQ", "QJ", "QT", "JJ", "JT", "TT", "99"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A7", "A6", "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4",
                        "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "55", "54", "53", "52", "44", "43", "42", "33", "32", "22"
                    })
                };

        }

        // BB vs SB ALL-IN
        public static List<KeyValuePair<string, List<string>>> GetBBvsSBAllInSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "KQ", "KJ", "QJ", "QT", "JT"
                    })
                };
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "QJ", "QT", "Q9", "JT", "J9", "T9"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2",
                        "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "98"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetBBvsSBAllInOffSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 12 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44"
                    })
                };
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "QQ", "QJ", "QT", "JJ", "TT", "99", "88", "77", "66", "55", "44"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                     new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QQ", "QJ", "QT", "Q9", "Q8", "Q7", "JJ",
                        "JT", "J9", "J8", "TT", "T9", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
        }

    }
}