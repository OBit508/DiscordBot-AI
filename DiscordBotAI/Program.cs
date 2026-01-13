using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using System.Collections.Concurrent;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace CoelhoBot
{
    public static class Program
    {
        public static HttpClient Http = new HttpClient();
        public static DiscordClient BotClient;
        public static List<Message> Pendent = new List<Message>();
        public static List<string> PendentSay = new List<string>();
        public static bool ChangingMemory;
        public static bool ResetingMemory;
        public static ConcurrentQueue<Func<Task>> DelayedTasks = new ConcurrentQueue<Func<Task>>();
        public static DiscordRole AmigoDoCoelho => BotClient.Guilds[1347318199586259036].GetRole(1456083211405754491);
        public static DiscordChannel LogChannel => BotClient.Guilds[1347318199586259036].GetChannel(1456085997455544474);
        public static DiscordChannel SaveChannel => BotClient.Guilds[1347318199586259036].GetChannel(1457234355410960464);
        public static DiscordChannel Channel => BotClient.Guilds[1347318199586259036].GetChannel(1456075446125858908);
        public static async Task Main()
        {
            MemoryManager.Initialize();
            BotClient = new DiscordClient(new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = MemoryManager.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            });
            BotClient.UseSlashCommands().RegisterCommands<Slash>();
            await BotClient.ConnectAsync();
            BotClient.MessageCreated += async (DiscordClient discordClient, MessageCreateEventArgs e) =>
            {
                if (e.Author.IsBot || e.Channel.Id != 1456075446125858908)
                {
                    return;
                }
                Pendent.Add(new Message() { Role = "user", Content = $"[Nome: {e.Author.Username}, ID: {e.Author.Id}]: {await ReplaceMentionsAsync(e.Message.Content)}" });
            };
            BotClient.ComponentInteractionCreated += async (DiscordClient discordClient, ComponentInteractionCreateEventArgs componentInteractionCreateEvent) =>
            {
                if (componentInteractionCreateEvent.Id == "load_memory" && componentInteractionCreateEvent.Message.Attachments.Count == 1)
                {
                    try
                    {
                        ChangingMemory = true;
                        while (ChangingMemory) { }
                        MemoryManager.Deserialize(await Http.GetByteArrayAsync(componentInteractionCreateEvent.Message.Attachments[0].Url), out string result, out DiscordColor color);
                        await componentInteractionCreateEvent.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder()
                        {
                            Title = "[Resultado]",
                            Description = result,
                            Color = color
                        }));
                    }
                    catch
                    {
                        await componentInteractionCreateEvent.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder()
                        {
                            Title = "[Resultado]",
                            Description = "Não foi possível ler o arquivo de memória.",
                            Color = DiscordColor.Red
                        }));
                    }
                }
            };
            BotClient.Ready += async (DiscordClient discordClient, ReadyEventArgs readyEventArgs) =>
            {
                while (true)
                {
                    if (Pendent.Count > 0)
                    {
                        foreach (Message message in Pendent)
                        {
                            MemoryManager.Add(message);
                        }
                        Pendent.Clear();
                        string result = await OpenRouterManager.SendChatAsync(MemoryManager.Mem.Messages);
                        MemoryManager.Add(new Message() { Role = "assistant", Content = result });
                        await Channel.SendMessageAsync(result);
                    }
                    if (PendentSay.Count > 0)
                    {
                        foreach (string say in PendentSay)
                        {
                            MemoryManager.Add(new Message() { Role = "assistant", Content = say });
                            await Channel.SendMessageAsync(say);
                        }
                        PendentSay.Clear();
                    }
                    if (ResetingMemory)
                    {
                        MemoryManager.Mem = new MemoryManager.BotMemory() { Messages = new List<Message>() { MemoryManager.Default } };
                        File.WriteAllText(MemoryManager.SavePath, JsonSerializer.Serialize(MemoryManager.Mem));
                        ResetingMemory = false;
                    }
                    if (ChangingMemory)
                    {
                        ChangingMemory = false;
                    }
                    await Task.Delay(333);
                }
            };
            Task.Run(async () =>
            {
                while (true)
                {
                    if (DelayedTasks.TryDequeue(out Func<Task> task))
                    {
                        await task();
                    }
                    await Task.Delay(200);
                }
            });
            await Task.Delay(-1);
        }
        public static async Task<string> ReplaceMentionsAsync(string text)
        {
            MatchCollection matches = Regex.Matches(text, @"<@!?(\d+)>");
            if (matches.Count == 0)
            {
                return text;
            }
            Dictionary<string, string> replacements = new Dictionary<string, string>();
            foreach (Match match in matches)
            {
                string id = match.Groups[1].Value;
                if (replacements.ContainsKey(id))
                {
                    continue;
                }
                DiscordUser user = null;
                if (id == BotClient.CurrentUser.Id.ToString())
                {
                    user = BotClient.CurrentUser;
                }
                else
                {
                    try
                    {
                        user = await BotClient.GetUserAsync(ulong.Parse(id));
                    }
                    catch { }
                }
                replacements[id] = $"@[Nome: {(user == BotClient.CurrentUser ? "Algodão" : user?.Username) ?? "Unknown"}, ID: {id}]";
            }
            foreach (KeyValuePair<string, string> pair in replacements)
            {
                text = Regex.Replace(text, $@"<@!?{pair.Key}>", pair.Value);
            }
            return text;
        }
        public static void LogInfo(string message)
        {
            DelayedTasks.Enqueue(new Func<Task>(async () =>
            {
                await LogChannel.SendMessageAsync(new DiscordEmbedBuilder()
                {
                    Title = "[Info]",
                    Description = message,
                    Color = DiscordColor.Gray
                });
            }));
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("[Info] " + message);
            Console.ResetColor();
        }
    }
    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
    public class OpenRouterRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; }
    }
    public class Choice
    {
        [JsonPropertyName("message")]
        public Message Message { get; set; }
    }
    public class OpenRouterResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; }
    }
}
