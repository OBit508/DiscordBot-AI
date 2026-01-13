using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace CoelhoBot
{
    public class Slash : ApplicationCommandModule
    {
        [SlashCommand("reset", "Apague a memória do coelho")]
        public async Task Reset(InteractionContext ctx)
        {
            if (ctx.Member.Roles.Contains(Program.AmigoDoCoelho))
            {
                Program.ResetingMemory = true;
                while (Program.ResetingMemory) { }
                await ctx.CreateResponseAsync("Memória resetada...", true);
                if (ctx.Member.Id == 986740452969562112)
                {
                    return;
                }
                Program.LogInfo(ctx.Member.Mention + " Resetou a memória do coelho");
            }
            else
            {
                await ctx.CreateResponseAsync("Você não é " + Program.AmigoDoCoelho.Mention + " 😡", true);
            }
        }
        [SlashCommand("implant", "Implante uma memória false no Coelho")]
        public async Task Implant(InteractionContext ctx, [Option("memoria", "Escreva uma memória para o Coelho como se fosse ele pensando")] string memory)
        {
            if (ctx.Member.Roles.Contains(Program.AmigoDoCoelho))
            {
                MemoryManager.Add(new Message() { Role = "system", Content = "Você se lembra disso agora (" + await Program.ReplaceMentionsAsync(memory) + ")" });
                await ctx.CreateResponseAsync("Memória implantada", true);
                if (ctx.Member.Id == 986740452969562112)
                {
                    return;
                }
                Program.LogInfo(ctx.Member.Mention + " Implantou uma memória no coelho.\nMemória: " + memory);
            }
            else
            {
                await ctx.CreateResponseAsync("Você não é " + Program.AmigoDoCoelho.Mention + " 😡", true);
            }
        }
        [SlashCommand("say", "Faça o Coelho falar algo")]
        public async Task Say(InteractionContext ctx, [Option("mensagem", "Mensagem que o coelho irá enviar")] string message)
        {
            if (ctx.Member.Roles.Contains(Program.AmigoDoCoelho))
            {
                Program.PendentSay.Add(await Program.ReplaceMentionsAsync(message));
                await ctx.CreateResponseAsync("Assim que possível o Coelho irá repetir isso", true);
                if (ctx.Member.Id == 986740452969562112)
                {
                    return;
                }
                Program.LogInfo(ctx.Member.Mention + " usou say\nMensagem: " + message);
            }
            else
            {
                await ctx.CreateResponseAsync("Você não é " + Program.AmigoDoCoelho.Mention + " 😡", true);
            }
        }
        [SlashCommand("save", "Salve a memória atual do Coelho")]
        public async Task Save(InteractionContext ctx, [Option("nome", "Nome do salvamento")] string name)
        {
            if (ctx.Member.Roles.Contains(Program.AmigoDoCoelho))
            {
                using (MemoryStream ms = new MemoryStream(MemoryManager.Serialize()))
                {
                    DiscordMessage discordMessage = await Program.SaveChannel.SendMessageAsync(new DiscordMessageBuilder()
                    {
                        Embed = new DiscordEmbedBuilder()
                        {
                            Title = "[Save]",
                            Description = "Nome do save: " + name,
                            Color = DiscordColor.Green
                        },
                    }.AddFile("Memória - " + MemoryManager.MemoryVersion, ms).AddComponents(new DiscordButtonComponent(ButtonStyle.Success, "load_memory", "Carregar")));
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder()
                    {
                        Title = "[Save]",
                        Description = "Nome do save: " + name + "\n" + discordMessage.JumpLink,
                        Color = DiscordColor.Green
                    }).AsEphemeral());
                }
            }
            else
            {
                await ctx.CreateResponseAsync("Você não é " + Program.AmigoDoCoelho.Mention + " 😡", true);
            }
        }
    }
}
