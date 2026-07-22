namespace quizportal.Models;

public static class AppRoles
{
    public const int Student = 0;
    public const int Teacher = 1;
    public const int Admin = 2;

    public const string StudentName = "Student";
    public const string TeacherName = "Teacher";
    public const string AdminName = "Admin";

    public const string ContentManagers = $"{TeacherName},{AdminName}";

    public static string ToRoleName(int userRole) => userRole switch
    {
        Admin => AdminName,
        Teacher => TeacherName,
        _ => StudentName
    };

    public static int FromRoleName(string roleName)
    {
        if (string.Equals(roleName, AdminName, StringComparison.OrdinalIgnoreCase))
            return Admin;
        if (string.Equals(roleName, TeacherName, StringComparison.OrdinalIgnoreCase))
            return Teacher;
        return Student;
    }
}
