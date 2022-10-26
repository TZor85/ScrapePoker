﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Enums
{
    public static class Actions2Handed
    {
        // OPEN RAISE
        public static List<KeyValuePair<string, List<string>>> GetOpenRaiseSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AK", "AQ", "AJ", "KQ", "KJ", "QJ"
                    }),
                    new KeyValuePair<string, List<string>>("OR / C / F", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "KT", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "87", "86", "85", "84", "83", "76", "75", "74",
                        "65", "64", "54"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / C", new List<string>
                    {
                        "K9", "K8", "Q9", "J9", "T9"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "92", "82", "73", "72", "63", "62", "53", "52", "43", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AT", "A9", "A8", "A7"
                    })
                };
            else if (stack > 15 && stack <= 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AK", "AQ", "AJ", "KQ", "KJ", "QJ"
                    }),
                    new KeyValuePair<string, List<string>>("OR / C / F", new List<string>
                    {
                        "A5", "A4", "A3", "A2", "KT", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "87", "86", "85", "84", "83", "76", "75", "74",
                        "65", "64", "54"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / C", new List<string>
                    {
                        "K9", "Q9", "J9", "T9"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "92", "82", "73", "72", "63", "62", "53", "52", "43", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AT", "A9", "A8", "A7", "A6"
                    })
                };
            else if (stack > 14 && stack <= 15)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AK", "AQ", "AJ", "KQ", "KJ", "QJ"
                    }),
                    new KeyValuePair<string, List<string>>("OR / C / F", new List<string>
                    {
                        "KT", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "A5", "A4", "A3", "A2", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "98", "97", "96", "95", "94", "87", "86", "85", "84", "76", "75", "74",
                        "65", "64", "54"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / C", new List<string>
                    {
                        "K9", "Q9", "J9", "T9"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "T3", "T2", "93", "92", "83", "82", "73", "72", "63", "62", "53", "52", "43", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AT", "A9", "A8", "A7", "A6"
                    })
                };
            else if (stack > 12 && stack <= 14)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AK", "AQ", "AJ", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "T8", "T7", "T6", "T5", "98", "97", "96", "95", "87", "86", "85", "76", "75", "74", "65", "64", "54"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / C", new List<string>
                    {
                        "K9", "Q9", "J9", "T9"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "J3", "J2", "T4", "T3", "T2", "94", "93", "92", "84", "83", "82", "73", "72", "63", "62", "53", "52", "43", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2",
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AK", "AQ", "AJ", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "T8", "T7", "T6", "T5", "98", "97", "96", "95", "87", "86", "85", "76", "75", "74", "65", "64", "54", "53", "43"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / C", new List<string>
                    {
                        "K9", "Q9", "J9", "T9"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "J3", "J2", "T4", "T3", "T2", "94", "93", "92", "84", "83", "82", "73", "72", "63", "62", "52", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2",
                    })
                };
            else if (stack > 8 && stack <= 10)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AK", "AQ", "AJ", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "Q5", "J7", "J6", "J5", "T7", "T6", "T5", "97", "96", "95", "85", "74", "64", "53", "43"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "Q4", "Q3", "Q2", "J4", "J3", "J2", "T4", "T3", "T2", "94", "93", "92", "84", "83", "82", "73", "72", "63", "62", "52", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "J8", "T8", "98", "87", "86", "76", "75", "65", "54"
                    })
                };
            else if (stack > 6 && stack <= 8)
                return new List<KeyValuePair<string, List<string>>>
                {                   
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "74", "53", "43"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "Q5", "Q4", "Q3", "Q2", "J5", "J4", "J3", "J2", "T5", "T4", "T3", "T2", "95", "94", "93", "92", "85", "84", "83", "82", "73", "72", "63", "62", "52", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6",
                        "98", "97", "96", "87", "86", "76", "75", "65", "64", "54"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "93", "92", "83", "82", "73", "72", "62", "52", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", 
                        "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "87", "86", "85", "84", "76", "75", "74", "65", "64", "63", "54", "53", "43"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetOpenRaiseOffSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ", "TT", "99", "88", "77"
                    }),
                    new KeyValuePair<string, List<string>>("OR / C / F", new List<string>
                    {
                        "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "K9", "K8", "Q9", "Q8", "J9", "J8", "T9", "T8", "98"                        
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "K7", "K6", "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "Q3", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "T2", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82",
                         "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "66", "55", "44", "33", "22"
                    })
                };
            else if (stack > 15 && stack <= 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ", "TT", "99", "88"
                    }),
                    new KeyValuePair<string, List<string>>("OR / C / F", new List<string>
                    {
                        "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "A5", "A4", "A3", "A2", "K9", "K8", "Q9", "Q8", "J9", "J8", "T9", "T8", "98"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "K7", "K6", "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "Q3", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "T2", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82",
                         "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "77", "66", "55", "44", "33", "22"
                    })
                };
            else if (stack > 14 && stack <= 15)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AA", "KK", "KQ", "QQ", "JJ", "TT", "99", "88"
                    }),
                    new KeyValuePair<string, List<string>>("OR / C / F", new List<string>
                    {
                        "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "A5", "A4", "A3", "A2", "K9", "K8", "Q9", "Q8", "J9", "J8", "T9", "T8", "98"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "K7", "K6", "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "Q3", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "T2", "97", "96", "95", "94", "87", "86", "85", "84", "76", "75", "74", "73", "65", "64", "63",
                        "62", "54", "53", "52", "43", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "77", "66", "55", "44", "33", "22"
                    })
                };
            else if (stack > 12 && stack <= 14)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT", "99"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "A5", "A4", "A3", "A2", "KT", "K9", "K8", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "98"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "K7", "K6", "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "Q3", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "97", "96", "95", "94", "87", "86", "85", "84", "76", "75", "74", "73", "65", "64", "63",
                        "54", "53", "43", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AA", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT", "99"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "KT", "K9", "K8", "QT", "Q9", "Q8", "JT", "J9", "J8", "T9", "T8", "98", "87", "76"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "K7", "K6", "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "Q3", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "97", "96", "95", "94", "86", "85", "84", "75", "74", "73", "65", "64", "63", "54", "53", "43", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
            else if (stack > 8 && stack <= 10)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("OR / 4B / C", new List<string>
                    {
                        "AA", "KK", "KQ", "KJ", "KT", "QQ", "QJ", "QT", "JJ", "JT", "TT", "99"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "K9", "K8", "Q9", "Q8", "J9", "J8", "T9", "T8", "98", "87", "76"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "K7", "K6", "K5", "K4", "K3", "K2", "Q7", "Q6", "Q5", "Q4", "Q3", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "97", "96", "95", "94", "86", "85", "84", "75", "74", "73", "65", "64", "63", "54", "53", "43", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
            else if (stack > 6 && stack <= 8)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("L / C / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ"
                    }),
                    new KeyValuePair<string, List<string>>("L / C / F", new List<string>
                    {
                        "87", "76"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "Q7", "Q6", "Q5", "Q4", "Q3", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "97", "96", "95", "94", "86", "85", "84", "75", "74", "73", "65", "64", "63", "54", "53", "43", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "JT", "J9", "J8", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("L / C / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ"
                    }),
                    new KeyValuePair<string, List<string>>("L / F", new List<string>
                    {
                        "J5", "J4", "J3", "J2", "T5", "T4", "T3", "95", "94", "85", "84", "75", "74", "73", "65", "64", "63", "54", "53", "43", "42", "32"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6",
                        "TT", "T9", "T8", "T7", "T6", "99", "98", "97", "96", "88", "87", "86", "77", "76", "66", "55", "44", "33", "22"
                    })
                };
        }

        // vs LIMP
        public static List<KeyValuePair<string, List<string>>> GetVsLimpSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AK", "AQ", "KQ", "KJ", "KT", "QJ", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92",
                        "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if ((stack > 15 && stack <= 20) || (stack > 14 && stack <= 15) || (stack > 12 && stack <= 14))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AK", "AQ", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82",
                        "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                   new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82",
                        "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if (stack > 8 && stack <= 10)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82",
                        "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if (stack > 6 && stack <= 8)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "JT", "J9", "J8", "T9", "T8", "98", "87", "76"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "Q5", "Q4", "Q3", "Q2", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "T2", "97", "96", "95", "94", "93", "92", "86", "85", "84", "83", "82", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "T9", "T8", "T7", "T6",
                        "98", "97", "96", "87", "86", "76", "75", "65", "64", "54"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "J4", "J3", "J2", "T5", "T4", "T3", "T2", "95", "94", "93", "92", "85", "84", "83", "82", "74", "73", "72", "63", "62", "53", "52", "43", "42", "32"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetVsLimpOffSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ", "TT", "99", "88", "77"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92",
                        "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if ((stack > 15 && stack <= 20) || (stack > 14 && stack <= 15))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ", "TT", "99", "88", "77"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "QJ", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92",
                        "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if (stack > 12 && stack <= 14)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "KT", "K9", "QQ", "QT", "Q9", "JJ", "JT", "J9", "TT", "T9", "99"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "QJ", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92",
                        "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "KT", "QQ", "QT", "JJ", "JT", "TT", "99"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "QJ", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92",
                        "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if (stack > 8 && stack <= 10)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ", "TT", "99"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "QJ", "QT", "JT", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92",
                        "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if (stack > 6 && stack <= 8)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ", "TT", "99"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "K8", "QJ", "QT", "JT", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92",
                        "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
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
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "JT", "J9", "J8", "T9", "T8",
                        "99", "98", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "Q4", "Q3", "Q2", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "T2", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54",
                        "53", "52", "43", "42", "32"
                    })
                };
        }

        // vs OPEN RAISE
        public static List<KeyValuePair<string, List<string>>> GetVsOpenRaiseSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2",
                        "98", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if ((stack > 15 && stack <= 20) || (stack > 14 && stack <= 15))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "QJ", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85",
                        "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if (stack > 12 && stack <= 14)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "QJ"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "JT", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85",
                        "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if ((stack > 10 && stack <= 12) || (stack > 8 && stack <= 10))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "Q9", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J9", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T9", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85",
                        "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else if (stack > 6 && stack <= 8)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "QJ", "QT", "Q9", "JT", "J9", "T9"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K7", "K6", "K5", "K4", "K3", "K2", "Q8", "Q7", "Q6", "Q5", "Q4", "Q3", "Q2", "J8", "J7", "J6", "J5", "J4", "J3", "J2", "T8", "T7", "T6", "T5", "T4", "T3", "T2", "98", "97", "96", "95", "94", "93", "92", "87", "86", "85",
                        "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62", "54", "53", "52", "43", "42", "32"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "JT", "J9", "J8", "T9", "T8", "98"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "Q4", "Q3", "Q2", "J7", "J6", "J5", "J4", "J3", "J2", "T7", "T6", "T5", "T4", "T3", "T2", "97", "96", "95", "94", "93", "92", "87", "86", "85", "84", "83", "82", "76", "75", "74", "73", "72", "65", "64", "63", "62",
                        "54", "53", "52", "43", "42", "32"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetVsOpenRaiseOffSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "KQ", "QQ", "JJ", "TT"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A7", "A6", "A5", "A4", "A3", "A2", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6", "98", "97", "87", "76"
                    })
                };
            else if ((stack > 15 && stack <= 20) || (stack > 14 && stack <= 15))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "KJ", "KT", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A6", "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6", "98", "97", "87", "76"
                    })
                };
            else if (stack > 12 && stack <= 14)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("3BB / C / C", new List<string>
                    {
                        "AA", "KK", "QQ", "JJ"
                    }),
                    new KeyValuePair<string, List<string>>("ALL-IN", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "KQ", "KJ", "KT", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "A5", "A4", "A3", "A2", "K9", "K8", "K7", "K6", "K5", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6", "98", "97", "87", "76"
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
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "TT", "99", "88", "77", "66", "55", "44", "33", "22"
                    }),
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "K9", "K8", "K7", "K6", "K5", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "JT", "J9", "J8", "J7", "J6", "T9", "T8", "T7", "T6", "98", "97", "87", "76"
                    })
                };
        }

        // vs ALL-IN
        public static List<KeyValuePair<string, List<string>>> GetVsAllInSuitedAction(double stack)
        {
            if (stack > 20 || (stack > 15 && stack <= 20))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "KQ", "KJ", "KT", "QJ", "QT", "JT"
                    })
                };
            else if ((stack > 14 && stack <= 15) || (stack > 12 && stack <= 14))
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "KQ", "KJ", "KT", "K9", "QJ", "QT", "Q9", "JT", "J9", "T9"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "KQ", "KJ", "KT", "K9", "K8", "QJ", "QT", "Q9", "JT", "J9", "T9"
                    })
                };
            else if (stack > 8 && stack <= 10)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "QJ", "QT", "Q9", "JT", "J9", "T9"
                    })
                };
            else if (stack > 6 && stack <= 8)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "JT", "J9", "J8", "T9", "T8", "98"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "K2", "QJ", "QT", "Q9", "Q8", "Q7", "Q6", "Q5", "JT", "J9", "J8", "J7", "T9", "T8", "98"
                    })
                };
        }
        public static List<KeyValuePair<string, List<string>>> GetVsAllInOffSuitedAction(double stack)
        {
            if (stack > 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44"
                    })
                };
            else if (stack > 15 && stack <= 20)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "KK", "QQ", "JJ", "TT", "99", "88", "77", "66", "55", "44"
                    })
                };
            else if (stack > 14 && stack <= 15)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "KK", "QQ", "QJ", "JJ", "JT", "TT", "99", "88", "77", "66", "55", "44", "33"
                    })
                };
            else if (stack > 12 && stack <= 14)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "KK", "KQ", "KJ", "QQ", "QJ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33"
                    })
                };
            else if (stack > 10 && stack <= 12)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "QQ", "QJ", "JJ", "TT", "99", "88", "77", "66", "55", "44", "33"
                    })
                };
            else if (stack > 8 && stack <= 10)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "QQ", "QJ", "JJ", "TT", "T9", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
            else if (stack > 6 && stack <= 8)
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "QQ", "QJ", "QT", "Q9", "Q8", "JJ", "JT", "J9", "TT", "T9", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
            else
                return new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("CALL", new List<string>
                    {
                        "AA", "AK", "AQ", "AJ", "AT", "A9", "A8", "A7", "A6", "A5", "A4", "A3", "A2", "KK", "KQ", "KJ", "KT", "K9", "K8", "K7", "K6", "K5", "K4", "K3", "QQ", "QJ", "QT", "Q9", "Q8", "JJ", "JT", "J9", "TT", "T9", "99", "88", "77", "66", "55", "44", "33", "22"
                    })
                };
        }

    }
}
