namespace ClientLauncher.Implement.ViewModels.Response
{
    public class TheGrandEmployeeResponse
    {
        public string employeeID { get; set; }
        public string fullName { get; set; }
        public string departmentName { get; set; }
        public string position { get; set; }
        public string adUserName { get; set; }
        public bool adStatus { get; set; }
    }

    public class TheGrandEmployeeBaseResponse
    {
        public string result { get; set; }
        public List<TheGrandEmployeeResponse> data { get; set; }
    }
}