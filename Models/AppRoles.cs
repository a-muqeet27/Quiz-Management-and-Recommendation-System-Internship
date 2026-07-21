namespace quizportal.Models;

public static class AppRoles
{
    public const int Student = 0;
    public const int Teacher = 1;

    public const string StudentName = "Student";
    public const string TeacherName = "Teacher";

    public static string ToRoleName(int userRole) => userRole switch
    {
        Teacher => TeacherName,
        _ => StudentName
    };

    public static int FromRoleName(string roleName) =>
        string.Equals(roleName, TeacherName, StringComparison.OrdinalIgnoreCase) ? Teacher : Student;
}
