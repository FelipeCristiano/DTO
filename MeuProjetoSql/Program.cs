using System;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        // Configuração da conexão com o banco de dados
        string connectionString = @"Server=DEVBDSRE\SQLDEV;Database=SREDesenv; Integrated Security=True; TrustServerCertificate=True;";

        // Query SQL para buscar os dados XML da tabela
        string query = @"
        SELECT XmlRFBS35
        FROM [SREDesenv].[dbo].[FilaS99]
        WHERE XmlRFBS35 LIKE '%<d4p1:string>%'
        ";

        
        Dictionary<string, int> eventCount = new Dictionary<string, int>();
       
        Dictionary<string, int> combinationCount = new Dictionary<string, int>();

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();
            SqlCommand cmd = new SqlCommand(query, conn);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string xmlData = reader["XmlRFBS35"].ToString();

                
                string pattern = @"<d4p1:string>(\d{3})</d4p1:string>";
                MatchCollection matches = Regex.Matches(xmlData, pattern);

                
                HashSet<string> uniqueEvents = new HashSet<string>();

                foreach (Match match in matches)
                {
                    string eventCode = match.Groups[1].Value;
                    uniqueEvents.Add(eventCode);
                }

                
                foreach (string eventCode in uniqueEvents)
                {
                    if (eventCount.ContainsKey(eventCode))
                        eventCount[eventCode]++;
                    else
                        eventCount[eventCode] = 1;
                }

                
                if (uniqueEvents.Count > 1)
                {
                    
                    List<string> sortedEvents = uniqueEvents.OrderBy(e => e).ToList();
                    string combinationKey = string.Join(",", sortedEvents);

                    if (combinationCount.ContainsKey(combinationKey))
                        combinationCount[combinationKey]++;
                    else
                        combinationCount[combinationKey] = 1;
                }
            }

            reader.Close();
        }

        
        var sortedEventCount = eventCount.OrderByDescending(e => e.Value).ToList();
        
        var sortedCombinationCount = combinationCount.OrderByDescending(c => c.Value).ToList();

        
        var dto = new
        {
            eventos = sortedEventCount.Select(e => new { evento = e.Key, quantidade = e.Value }).ToList(),
            combinacoes = sortedCombinationCount.Select(c => new { combinacao = c.Key, quantidade = c.Value }).ToList()
        };

        
        string jsonFilePath = "XmlRFBS35 - Eventos.json";
        File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(dto, Formatting.Indented));

        Console.WriteLine($"DTO gerado e salvo em '{jsonFilePath}'");
    }
}
