namespace Core.Reports;

public sealed record ReaderLoanCount(string ReaderId, int Loans, int Open);

public sealed record OverdueLine(string LoanId, string CopyId, string ReaderId, DateOnly DueDate, int DaysOverdue);

public sealed record GroupSummary(string Key, int Count);

public sealed record CopyStats(int Titles, int Copies, int IssuedCopies, double AverageCopiesPerTitle, int MaxCopiesPerTitle);