namespace DelightsBoutique.Domain.Enums;

public enum Permission
{
    ManageProducts,
    ManageOrders,
    ManageCustomers,
    ManageContent,
    ViewReports,
    // Appended, not inserted — Permissions is stored as a Postgres integer[], so existing
    // rows' values must keep meaning what they already mean.
    ManageStaff,
    ViewAuditLog
}
