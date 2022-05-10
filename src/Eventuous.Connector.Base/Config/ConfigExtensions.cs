using Grpc.Core;

namespace Eventuous.Connector.Base.Config;

public static class ConfigExtensions {
    public static string GetHost(this GrpcProjectorSettings? settings)
        => Ensure.NotEmptyString(settings?.Uri, "gRPC projector URI");
    
    public static ChannelCredentials GetCredentials(this GrpcProjectorSettings? settings) {
        var setting = Ensure.NotEmptyString(settings?.Credentials, "gRPC projector credentials");

        return setting switch {
            "insecure" => ChannelCredentials.Insecure,
            "ssl"      => ChannelCredentials.SecureSsl,
            _          => throw new ArgumentOutOfRangeException(setting, "Unknown credentials")
        };
    }
}
