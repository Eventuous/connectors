// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Reflection;

namespace Eventuous.Connector.Base.Tools; 

static class TypeExtensionsForRegistrations {
    public static (ConstructorInfo Ctor, ParameterInfo Param)[] GetConstructors<T>(
        this Type type,
        string?   name = null
    )
        => type
            .GetConstructors()
            .Select(
                x => (
                    Ctor: x,
                    Options: x.GetParameters()
                        .SingleOrDefault(
                            y => y.ParameterType == typeof(T) && (name == null || y.Name == name)
                        )
                )
            ).Where(x => x.Options != null).ToArray()!;
}
