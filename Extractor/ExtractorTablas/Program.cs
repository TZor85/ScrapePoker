using ExtractorTablas;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

internal class Program
{
    private static void Main(string[] args)
    {

        string jsonFilePath = "tabla.txt"; // Ruta del archivo JSON
        string jsonText = File.ReadAllText(jsonFilePath); // Lee el contenido del archivo

        StreamWriter writer = new StreamWriter("tabla_formateada1.txt");

        var rootObject = JObject.Parse(jsonText);

        var texto = string.Empty;
        var dic = new Dictionary<string, string>();

        var hand = string.Empty;
        //texto = "\t\t\t{\n";
        foreach (var item in rootObject)
        {
            if (item.Key.Contains("box"))
            {
                hand = replaceBox(item.Key);
                var pp = item.Value["colorPercentage"].ToString().Replace("{", string.Empty).Replace("}", string.Empty).Split(",");

                foreach (var colors in pp)
                {
                    var col = colors.Replace("\r\n", string.Empty).Trim().Split(":");

                    texto += "\t\t\t\tnew Hands{ " + $"Hand = \"{hand}\", " + $"Action = {col[0]}, " + $"Porcentajes = {col[1]}" + " },\n";
                }
            }
            else
            {
                foreach (var actions in item.Value)
                {
                    dic.Add(actions["name"].ToString(), actions["color"].ToString());
                }

            }

        }

        foreach (var item in dic)
        {
            // Crear un objeto TextInfo con la cultura actual
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            string action = string.Empty;
            string color = string.Empty;

            if (item.Key.Contains("BET"))
            {
                action = ChangeParentesis(item.Key);
                color = item.Value;
            }
            else
            {
                // Convertir el texto a capital letter
                action = textInfo.ToTitleCase(item.Key.ToLower());
                color = item.Value;
            }
                
            texto = texto.Replace(color, action);

        }


        //texto += "\t\t\t};";

        writer.WriteLine(texto);

        writer.Close();


        // Abre el archivo de texto de origen
        //StreamReader reader = new StreamReader("tabla.txt");

        //// Crea un nuevo archivo de texto de destino
        //StreamWriter writer = new StreamWriter("tabla_formateada.txt");

        //// Lee cada línea del archivo de origen
        //string line;

        //var hand = string.Empty;

        //writer.WriteLine("var hands = new List<Hands>");
        //writer.WriteLine("\t\t\t{");
        //while ((line = reader.ReadLine()) != null)
        //{

        //    var action = new Dictionary<string, string>();

        //    if (line.Contains("box"))
        //        hand = replaceBox(line);

        //    if(!line.Contains("color") && line.Contains("{#"))
        //        action = replaceAction(line);

        //    foreach (var item in action)
        //        writer.WriteLine("\t\t\t\tnew Hands{ " + $"Hand = \"{hand}\", " + $"Action = \"{item.Key}\", " + $"Porcentajes = {item.Value.Split(",")[0]}" + " },");

        //    if(line.Contains("comboPercentage") && line.Contains("name"))
        //    {
        //        var actions = line.Split(":");
        //        writer.WriteLine($"{actions[1].Split(",")[0]} - {actions.Last()}");
        //    }

        //}
        //writer.WriteLine("\t\t\t};");

        //// Cierra los archivos
        //reader.Close();
        //writer.Close();
    }

    private static string ChangeParentesis(string texto)
    {   
        string nuevoTexto = string.Empty;

        // Buscar el patrón "númeroBET (número BB)" usando una expresión regular
        string patron = @"(\d+)BET \((\d+) BB\)";
        Regex regex = new Regex(patron);
        Match match = regex.Match(texto);

        if (match.Success)
        {
            // Obtener los números capturados por la expresión regular
            string numero1 = match.Groups[1].Value;
            string numero2 = match.Groups[2].Value;

            // Construir el nuevo texto utilizando los números capturados
            nuevoTexto = $"{numero1}Bet x{numero2}";
        }

        return nuevoTexto;
    }

    private static Dictionary<string, string> replaceAction(string color)
    {
        var actions = color.Replace("{", "").Replace("}", "");
        var dict = new Dictionary<string, string>();

        var action = actions.Split(",");

        foreach (var item in action)
        {
            if (item.Contains("#3d3d3d"))
                dict.Add("All-In", item.Split(":")[1]);

            if (item.Contains("#e35353"))
                dict.Add("4Bet x__", item.Split(":")[1]);

            if (item.Contains("#8dd6f1"))
                dict.Add("Call", item.Split(":")[1]);

            if (item.Contains("#fff699"))
                dict.Add("Marginal Call", item.Split(":")[1]);

            if (item.Contains("#e0e0e0"))
                dict.Add("Fold", item.Split(":")[1]);
        }
        

        return dict;

    }

    private static string replaceBox(string box)
    {
        return box switch
        {
            "box0_0" => "AA",
            "box0_1" => "AKs",
            "box0_2" => "AQs",
            "box0_3" => "AJs",
            "box0_4" => "ATs",
            "box0_5" => "A9s",
            "box0_6" => "A8s",
            "box0_7" => "A7s",
            "box0_8" => "A6s",
            "box0_9" => "A5s",
            "box0_10" => "A4s",
            "box0_11" => "A3s",
            "box0_12" => "A2s",
            "box1_0" => "AKo",
            "box1_1" => "KK",
            "box1_2" => "KQs",
            "box1_3" => "KJs",
            "box1_4" => "KTs",
            "box1_5" => "K9s",
            "box1_6" => "K8s",
            "box1_7" => "K7s",
            "box1_8" => "K6s",
            "box1_9" => "K5s",
            "box1_10" => "K4s",
            "box1_11" => "K3s",
            "box1_12" => "K2s",
            "box2_0" => "AQo",
            "box2_1" => "KQo",
            "box2_2" => "QQ",
            "box2_3" => "QJs",
            "box2_4" => "QTs",
            "box2_5" => "Q9s",
            "box2_6" => "Q8s",
            "box2_7" => "Q7s",
            "box2_8" => "Q6s",
            "box2_9" => "Q5s",
            "box2_10" => "Q4s",
            "box2_11" => "Q3s",
            "box2_12" => "Q2s",
            "box3_0" => "AJo",
            "box3_1" => "KJo",
            "box3_2" => "QJo",
            "box3_3" => "JJ",
            "box3_4" => "JTs",
            "box3_5" => "J9s",
            "box3_6" => "J8s",
            "box3_7" => "J7s",
            "box3_8" => "J6s",
            "box3_9" => "J5s",
            "box3_10" => "J4s",
            "box3_11" => "J3s",
            "box3_12" => "J2s",
            "box4_0" => "ATo",
            "box4_1" => "KTo",
            "box4_2" => "QTo",
            "box4_3" => "JTo",
            "box4_4" => "TT",
            "box4_5" => "T9s",
            "box4_6" => "T8s",
            "box4_7" => "T7s",
            "box4_8" => "T6s",
            "box4_9" => "T5s",
            "box4_10" => "T4s",
            "box4_11" => "T3s",
            "box4_12" => "T2s",
            "box5_0" => "A9o",
            "box5_1" => "K9o",
            "box5_2" => "Q9o",
            "box5_3" => "J9o",
            "box5_4" => "T9o",
            "box5_5" => "99",
            "box5_6" => "98s",
            "box5_7" => "97s",
            "box5_8" => "96s",
            "box5_9" => "95s",
            "box5_10" => "94s",
            "box5_11" => "93s",
            "box5_12" => "92s",
            "box6_0" => "A8o",
            "box6_1" => "K8o",
            "box6_2" => "Q8o",
            "box6_3" => "J8o",
            "box6_4" => "T8o",
            "box6_5" => "98o",
            "box6_6" => "88",
            "box6_7" => "87s",
            "box6_8" => "86s",
            "box6_9" => "85s",
            "box6_10" => "84s",
            "box6_11" => "83s",
            "box6_12" => "82s",
            "box7_0" => "A7o",
            "box7_1" => "K7o",
            "box7_2" => "Q7o",
            "box7_3" => "J7o",
            "box7_4" => "T7o",
            "box7_5" => "97o",
            "box7_6" => "87o",
            "box7_7" => "77",
            "box7_8" => "76s",
            "box7_9" => "75s",
            "box7_10" => "74s",
            "box7_11" => "73s",
            "box7_12" => "72s",
            "box8_0" => "A6o",
            "box8_1" => "K6o",
            "box8_2" => "Q6o",
            "box8_3" => "J6o",
            "box8_4" => "T6o",
            "box8_5" => "96o",
            "box8_6" => "86o",
            "box8_7" => "76o",
            "box8_8" => "66",
            "box8_9" => "65s",
            "box8_10" => "64s",
            "box8_11" => "63s",
            "box8_12" => "62s",
            "box9_0" => "A5o",
            "box9_1" => "K5o",
            "box9_2" => "Q5o",
            "box9_3" => "J5o",
            "box9_4" => "T5o",
            "box9_5" => "95o",
            "box9_6" => "85o",
            "box9_7" => "75o",
            "box9_8" => "65o",
            "box9_9" => "55",
            "box9_10" => "54s",
            "box9_11" => "53s",
            "box9_12" => "52s",
            "box10_0" => "A4o",
            "box10_1" => "K4o",
            "box10_2" => "Q4o",
            "box10_3" => "J4o",
            "box10_4" => "T4o",
            "box10_5" => "94o",
            "box10_6" => "84o",
            "box10_7" => "74o",
            "box10_8" => "64o",
            "box10_9" => "54o",
            "box10_10" => "44",
            "box10_11" => "43s",
            "box10_12" => "42s",
            "box11_0" => "A3o",
            "box11_1" => "K3o",
            "box11_2" => "Q3o",
            "box11_3" => "J3o",
            "box11_4" => "T3o",
            "box11_5" => "93o",
            "box11_6" => "83o",
            "box11_7" => "73o",
            "box11_8" => "63o",
            "box11_9" => "53o",
            "box11_10" => "43o",
            "box11_11" => "33",
            "box11_12" => "32s",
            "box12_0" => "A2o",
            "box12_1" => "K2o",
            "box12_2" => "Q2o",
            "box12_3" => "J2o",
            "box12_4" => "T2o",
            "box12_5" => "92o",
            "box12_6" => "82o",
            "box12_7" => "72o",
            "box12_8" => "62o",
            "box12_9" => "52o",
            "box12_10" => "42o",
            "box12_11" => "32o",
            "box12_12" => "22",

            _ => box
        };
    }
}