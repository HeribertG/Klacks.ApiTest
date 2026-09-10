// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Offline stand-in for the Nominatim-backed geocoding service. The HTTP-level tests must not depend on
 * the public Nominatim instance: requests from the CI runners do not get a usable match, so every
 * address POST/PUT was answered with 400 there. Every address resolves as an exact hit at fixed
 * coordinates; no state is returned, so the state cross-check in the address validation never fires.
 */

using Klacks.Api.Domain.Interfaces.RouteOptimization;

namespace Klacks.ApiTest.Infrastructure;

public class FakeGeocodingService : IGeocodingService
{
    private const double Latitude = 47.3769;
    private const double Longitude = 8.5417;
    private const string ExactMatchType = "exact";

    public Task<(double? Latitude, double? Longitude)> GeocodeAsync(
        string city, string country, CancellationToken cancellationToken = default) =>
        Task.FromResult<(double?, double?)>((Latitude, Longitude));

    public Task<(double? Latitude, double? Longitude)> GeocodeAddressAsync(
        string fullAddress, string country, CancellationToken cancellationToken = default) =>
        Task.FromResult<(double?, double?)>((Latitude, Longitude));

    public Task<GeocodingValidationResult> ValidateExactAddressAsync(
        string? street, string postalCode, string city, string country) =>
        Task.FromResult(new GeocodingValidationResult
        {
            Found = true,
            ExactMatch = true,
            Latitude = Latitude,
            Longitude = Longitude,
            ReturnedAddress = $"{street} {postalCode} {city}".Trim(),
            MatchType = ExactMatchType,
        });

    public Task<List<AddressSuggestion>> GetAddressSuggestionsAsync(
        string? street, string postalCode, string city, string country, int limit = 5) =>
        Task.FromResult(new List<AddressSuggestion>());
}
