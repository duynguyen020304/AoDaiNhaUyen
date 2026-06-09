namespace AoDaiNhaUyen.Application.Interfaces.Services;

public interface IPromptRedactionService
{
  string Redact(string? value, int maxLength = 2000);
}
