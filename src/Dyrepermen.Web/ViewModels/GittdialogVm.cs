using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

/// <summary>
/// Dialogen for "gitt i dag". <see cref="NesteDato"/> er det som star i
/// feltet: tomt forste gang, det brukeren skrev etter en valideringsfeil.
/// </summary>
public sealed record GittdialogVm(
    Gittgrunnlag Grunnlag,
    DateOnly? NesteDato,
    bool FraOversikt);
