namespace AoDaiNhaUyen.Api.Hermes;

public sealed record HermesParamDescription(
  string Name,
  string Type,
  bool Required,
  string Description);

public sealed record HermesFieldDescription(
  string Type,
  bool Required,
  string Description);

public sealed record HermesBodyDescription(
  string ContentType,
  bool Required,
  IReadOnlyDictionary<string, HermesFieldDescription> Schema,
  object? Example);

public sealed record HermesResponseDescription(
  string SuccessShape,
  string DataShape);

public sealed record HermesAdminApiDescription(
  string Method,
  string Route,
  string Purpose,
  IReadOnlyList<HermesParamDescription> PathParams,
  IReadOnlyList<HermesParamDescription> QueryParams,
  HermesBodyDescription? RequestBody,
  HermesResponseDescription ResponseBody,
  IReadOnlyList<string> NotesForAgent);
