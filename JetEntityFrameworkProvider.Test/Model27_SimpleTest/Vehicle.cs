namespace JetEntityFrameworkProvider.Test.Model27_SimpleTest
{
    class Vehicle
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Model { get; set; }
        public int BuildYear { get; set; }
        public virtual User User { get; set; }
    }
}