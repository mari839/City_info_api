using AutoMapper;

namespace CityInfo.API.Profiles
{
    public class CityProfile : Profile
    {
        public CityProfile() 
        { 
            CreateMap<Entities.City, Models.CityWithoutPointsOfInterestDto>(); //map property names on the source object to the same property names on the destination object.
                                                                               //if property doesn't exist it will be ignored.          
            CreateMap<Entities.City, Models.CityDto>();
        }
    }
}
