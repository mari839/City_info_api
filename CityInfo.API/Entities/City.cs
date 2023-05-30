using CityInfo.API.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CityInfo.API.Entities
{
    public class City
    {
        [Key] //our primary key is Id
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] //Id will be an identity column
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        public ICollection<PointOfInterest> PointOfInterest { get; set; } = new List<PointOfInterest>();
    
        public City(string name)
        {
            Name = name;
        }
    }
}
