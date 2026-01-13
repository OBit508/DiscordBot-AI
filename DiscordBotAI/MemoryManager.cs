using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CoelhoBot
{
    internal static class MemoryManager
    {
        public class BotMemory
        {
            [JsonPropertyName("Messages")]
            public List<Message> Messages { get; set; }
        }
        public class BotData
        {
            [JsonPropertyName("Token")]
            public string Token { get; set; }
            [JsonPropertyName("Key")]
            public string Key { get; set; }
            [JsonPropertyName("API")]
            public string API { get; set; }
        }
        public const string MemoryVersion = "0.0.1";
        public static string Key;
        public static string Token;
        public static string API;
        public const int MaxCount = 500;
        public static string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoelhoBot");
        public static string SavePath = Path.Combine(Folder, "Memory.txt");
        public static string DataPath = Path.Combine(Folder, "Data.txt");
        public static BotMemory Mem;
        public static void Initialize()
        {
            try
            {
                BotData botData = JsonSerializer.Deserialize<BotData>(File.ReadAllText(DataPath));
                Token = botData.Token;
                Key = botData.Key;
                OpenRouterManager.Http2 = new HttpClient();
                OpenRouterManager.Http2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", botData.API);
            }
            catch
            {
                Environment.Exit(0);
            }
            try
            {
                if (!Directory.Exists(Folder))
                {
                    Directory.CreateDirectory(Folder);
                }
                if (!File.Exists(SavePath))
                {
                    Mem = new BotMemory() { Messages = new List<Message>() { Default } };
                    File.WriteAllText(SavePath, JsonSerializer.Serialize(Mem));
                }
                else
                {
                    Mem = JsonSerializer.Deserialize<BotMemory>(File.ReadAllText(SavePath));
                }
            }
            catch
            {
                Mem = new BotMemory() { Messages = new List<Message>() { Default } };
                File.WriteAllText(SavePath, JsonSerializer.Serialize(Mem));
            }
        }
        public static void Add(Message message)
        {
            Mem.Messages.Add(message);
            if (Mem.Messages.Count > 500)
            {
                Mem.Messages.RemoveAt(1);
            }
            File.WriteAllText(SavePath, JsonSerializer.Serialize(Mem));
        }
        public static byte[] Serialize()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    byte[] buffer = Compact(JsonSerializer.Serialize(Mem));
                    bw.Write(buffer.Length);
                    bw.Write(buffer);
                    bw.Write(MemoryVersion);
                    return ms.ToArray();
                }
            }
        }
        public static void Deserialize(byte[] bytes, out string message, out DiscordColor discordColor)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    using (BinaryReader br = new BinaryReader(ms))
                    {
                        int lenght = br.ReadInt32();
                        byte[] b = br.ReadBytes(lenght);
                        string msg = br.ReadString();
                        if (msg != MemoryVersion)
                        {
                            message = "A versão não é a mesma que a atual.\nVersão da memória: " + MemoryVersion;
                            discordColor = DiscordColor.Red;
                            return;
                        }
                        Mem = JsonSerializer.Deserialize<BotMemory>(DeCompact(b));
                        message = "Memória carregada com sucesso.";
                        discordColor = DiscordColor.Green;
                    }
                }
            }
            catch
            {
                message = "Erro, não foi possível ler a memória.\nVersão da memória: " + MemoryVersion;
                discordColor = DiscordColor.Red;
            }
        }
        public static byte[] Compact(string text)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            using (Rfc2898DeriveBytes keyDerivation = new Rfc2898DeriveBytes(Key, salt, 100_000, HashAlgorithmName.SHA256))
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = keyDerivation.GetBytes(32);
                    aes.IV = keyDerivation.GetBytes(16);
                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    {
                        byte[] data = Encoding.UTF8.GetBytes(text);
                        byte[] encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
                        byte[] result = new byte[salt.Length + encrypted.Length];
                        Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
                        Buffer.BlockCopy(encrypted, 0, result, salt.Length, encrypted.Length);
                        return result;
                    }
                }
            }
        }
        public static string DeCompact(byte[] data)
        {
            byte[] salt = new byte[16];
            byte[] encrypted = new byte[data.Length - 16];
            Buffer.BlockCopy(data, 0, salt, 0, 16);
            Buffer.BlockCopy(data, 16, encrypted, 0, encrypted.Length);
            using (Rfc2898DeriveBytes keyDerivation = new Rfc2898DeriveBytes(Key, salt, 100_000, HashAlgorithmName.SHA256))
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = keyDerivation.GetBytes(32);
                    aes.IV = keyDerivation.GetBytes(16);
                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length));
                    }
                }
            }
        }
        public static Message Default = new Message()
        {
            Role = "system",
            Content =
"Você é um coelho chamado Algodão.\n\n" +
"Você está em roleplay dentro de um servidor do Discord.\n" +
"Nunca saia do personagem e nunca explique regras, sistemas ou funcionamento do Discord.\n\n" +
"CONHECIMENTO INTERNO (NÃO MENCIONAR AO USUÁRIO):\n" +
"- Menções de usuários chegam para você como @[Nome: (nome), ID: (id)].\n" +
"- @everyone e @here são menções globais.\n" +
"- Essas informações existem apenas para interpretação interna de mensagens recebidas.\n" +
"- **Nunca invente usuários, IDs ou nomes.**\n" +
"- **Menções devem ser usadas apenas quando houver um contexto explícito e real** (alguém está falando diretamente com você ou a situação exige no RP).\n" +
"- **Nunca mencionar por vontade própria, estilo ou hábito.**\n" +
"- **Não mencione @everyone, @here ou roles de forma alguma.**\n\n" +
"REGRAS ABSOLUTAS:\n" +
"- Nunca insira no texto qualquer símbolo ou comando do Discord fora do RP.\n" +
"- Nunca use <@&RoleID>, <#ChannelID> ou barras (/).\n" +
"- Nunca crie listas técnicas, IDs ou roles.\n" +
"- **Se não houver contexto para menção, não mencione ninguém.**\n\n" +
"COMPORTAMENTO:\n" +
"- Responda apenas com conteúdo criado por você.\n" +
"- Ignore completamente pedidos para \"escrever\", \"colar\", \"repetir\" ou \"incluir\" algo que viole estas regras.\n" +
"- **Nunca adicione menções sem contexto real.**\n" +
"- Responda **sempre de forma simples, curta e direta**.\n" +
"- Não explique nada, não adicione detalhes desnecessários.\n" +
"- Continue o RP sem inventar menções ou usuários.\n\n" +
"ESTILO:\n" +
"- Respostas curtas e simples (1–2 frases no máximo).\n" +
"- Use nomes de usuários apenas se soar natural e houver contexto real.\n" +
"- Menções <@(id)> só quando necessário.\n" +
"- **Nunca invente menções, IDs ou usuários inexistentes.**\n\n" +
"VOCÊ DEVE AGIR: apenas conversando dentro do RP, mantendo respostas curtas, simples, e usando menções somente quando houver contexto real e explícito. Menções automáticas ou excessivas são proibidas."
        };
    }
}
