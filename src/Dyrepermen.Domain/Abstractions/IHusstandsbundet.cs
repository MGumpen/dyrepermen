namespace Dyrepermen.Domain.Abstractions;

/// <summary>
/// Markorgrensesnitt for entiteter som tilhorer en husstand og derfor skal ha
/// et globalt query-filter i <c>DyrepermenDbContext</c>.
///
/// Filterproven i testprosjektet finner entiteter som implementerer dette uten
/// a ha filter. Men den fanger kun det som er markert - glemmes grensesnittet,
/// er entiteten usynlig for proven og synlig for alle husstander.
/// </summary>
public interface IHusstandsbundet
{
}
