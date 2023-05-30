using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CityInfo.API.Entities
{
    public class PointOfInterest
    {
        [Key] //our primary key is Id
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] //Id will be an identity column
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        [ForeignKey("CityId")]
        public City? City { get; set; } // <- navigation property. convention-based approach is when a relationship will be created when there is a navigation property discovered on a type.
                                        //A property is considered a navigation property if the type it points to cannot be mapped as a scalar type by the current database provider. Scalar types comprise enumeration types, integer types, and real types
        public int CityId { get; set; } //relationships that are discovered by convention will always have target the primary key of the principal entity
        public PointOfInterest(string name)
        {
            Name = name;
        }
    }
}
