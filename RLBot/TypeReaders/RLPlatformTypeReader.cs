using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using RLBot.API.RLS.Data;

namespace RLBot.TypeReaders
{
    public class RLPlatformTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            switch (input.ToLower())
            {
                case "pc":
                case "steam":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RlsPlatform.Steam));
                case "xbox":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RlsPlatform.Xbox));
                case "ps4":
                    return Task.FromResult(TypeReaderResult.FromSuccess(RlsPlatform.Ps4));
            }

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, $"Not a valid Platform. {string.Join(", ", Enum.GetValues(typeof(RlsPlatform)).Cast<RlsPlatform>())}"));
        }
    }
}