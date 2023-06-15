// Copyright (C) Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace Eventuous.Connector.Tools;

static class Ensure {
    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T NotNull<T>(T? value, [CallerArgumentExpression("value")] string? name = default) where T : class
        => value ?? throw new ArgumentNullException(name);

    [DebuggerHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string NotEmptyString(string? value, [CallerArgumentExpression("value")] string? name = default)
        => !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentNullException(name);
}