using ByteSizeLib;

namespace Borz.Core;

public record MemoryInfo(ByteSize Total, ByteSize Available);