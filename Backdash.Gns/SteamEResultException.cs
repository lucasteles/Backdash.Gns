// SPDX-FileCopyrightText: Copyright 2025 Guyeon Yu <copyrat90@gmail.com>
// SPDX-License-Identifier: 0BSD

namespace Backdash.Gns;

using System;
using GnsSharp;

/// <summary>
/// An exception that is thrown for <see cref="GnsSharp.EResult"/>.
/// </summary>
public class SteamEResultException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SteamEResultException"/> class.
    /// </summary>
    /// <param name="result">Result value.</param>
    internal SteamEResultException(EResult result)
        : base($"EResult.{result}")
    {
        this.Result = result;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SteamEResultException"/> class.
    /// </summary>
    /// <param name="result">Result value.</param>
    /// <param name="message">Additional message.</param>
    internal SteamEResultException(EResult result, string message)
    : base($"EResult.{result}: {message}")
    {
        this.Result = result;
    }

    /// <summary>
    /// Gets the result.
    /// </summary>
    public EResult Result { get; init; }
}
