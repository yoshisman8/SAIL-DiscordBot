using Discord;
using Discord.Commands;
using Discord.Addons.Interactive;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using LiteDB;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERA20.Modules
{
    public class HelpModule : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        private readonly CommandService _service;
        private readonly IConfiguration _config;

        public HelpModule(CommandService service, IConfiguration config)
        {
            _service = service;
            _config = config;
        }

        [Command("Commands")]
        public async Task HelpAsync()
        {
            string prefix = _config["prefix"];
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "These are the commands you can use"
            };
            
            foreach (var module in _service.Modules)
            {
                string description = null;
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                        description += $"{prefix}{cmd.Aliases.First()}\n";
                }
                
                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }

            await ReplyAsync("", false, builder.Build());
        }

        [Command("Commands"),Alias("Command")]
        public async Task HelpAsync(string command)
        {
            var result = _service.Search(Context, command);

            if (!result.IsSuccess)
            {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }

            string prefix = _config["prefix"];
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = $"Here are some commands like **{command}**"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;

                builder.AddField(x =>
                {
                    x.Name = string.Join(", ", cmd.Aliases);
                    x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" + 
                              $"Summary: {cmd.Summary}";
                    x.IsInline = false;
                });
            }

            await ReplyAsync("", false, builder.Build());
        }
        [Command("Help",RunMode = RunMode.Async)]
        public async Task HelpMenu(){
            var col = Database.GetCollection<HelpFile>("Helpfiles");
            var pages = new[]
            {
                new PaginatedMessage.Page{
                    Title = "About SAIL",
                    Description = "The Server Assistanting Intelligent Lattice (Or SAIL), is a bot specifically tailored to the needs of the Table Top Dragon Den server. Coded by Vyklade#0001.",
                    ThumbnailUrl = Context.Client.CurrentUser.GetAvatarUrl(),
                    TimeStamp = DateTimeOffset.UtcNow,
                    Color = Color.Teal
                },
                new PaginatedMessage.Page{
                    Title = "Rolling dice",
                    Description = "One of the most used features of this bot is dice rolling. In order to roll a dice, you must use the /roll (Or /r for short) command followed by a dice expression. Here is a list of valid die expressions:",
                    Fields = new List<EmbedFieldBuilder>
                    {
                        new EmbedFieldBuilder
                        {
                            Name = "Dice in the XdY form",
                            Value = "These will roll an X amount of die of Y faces. \nExamples:\n* 1d20\n* 2d6\n* 10d12.",
                            IsInline = true
                        },
                        new EmbedFieldBuilder{
                            Name = "Adding flat numbers to a dice.",
                            Value = "You can add or substract to the total dice roll by simply adding a +X or -Y to the dice roll, where X and Y are any number (or even other dice rolls!) that you want.\nExamples:\n* 1d20 + 5\n* 1d8 - 3",
                            IsInline = true
                        },
                        new EmbedFieldBuilder{
                            Name = "Success condition: Total is greater than",
                            Value = "If you add a > X (Where X is any number, no dice rolls here) at the end of a roll expression, you can add a success condition to your roll. And the bot will immedeately anounce if your roll beats that number or not.\n Examples:\n* 1d20 + 1d6 + 3 > 15",
                            IsInline = true
                        },
                        new EmbedFieldBuilder{
                            Name = "Success condition: Amount of successes",
                            Value = "If you instead add a >> X at the end of a die expperssion, you can evaluate every result of each individual dice to see if they roll above a certain number. And the bot will count the amount of successes obtained.\nExample:\n* 5d20 >> 5",
                            IsInline = true 
                        }
                    },
                    ThumbnailUrl = "https://www.freeiconspng.com/uploads/d20-icon-19.jpeg",
                    TimeStamp = DateTimeOffset.UtcNow,
                    Color = Color.Teal
                }
            };

            var Pager = new PaginatedMessage{

                Pages = pages,
                Title = "Help menu",
                Content = "Please wait for all reactions to load before navigating",
                Color = Color.Teal,
                Options = new PaginatedAppearanceOptions{
                    JumpDisplayOptions = JumpDisplayOptions.Always,
                    Timeout = TimeSpan.FromMinutes(3)
                },
                ThumbnailUrl = "https://melbournechapter.net/images/dictionary-clipart-research-finding-5.png"
            };
            await PagedReplyAsync(Pager, new ReactionList{
                First = true,
                Backward = true,
                Jump = true,
                Forward = true,
                Last = true,
                Trash = true,
            });
        }
    }

    public class HelpFile{
        public int Id {get;set;}
        public string Title {get;set;}
        public string Content {get;set;}
        public string[] Tags {get;set;}
    }
}
