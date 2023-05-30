using CityInfo.API.DbContexts;
using CityInfo.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace CityInfo.API.Services
{
    public class CityInfoRepository : ICityInfoRepository
    {
        private readonly CityInfoContext _context;
        public CityInfoRepository(CityInfoContext context) 
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));  
        }

        public async Task<IEnumerable<City>> GetCitiesAsync()
        {
            return await _context.Cities.OrderBy(c=>c.Name).ToListAsync();
        }
        public async Task<(IEnumerable<City>, PaginationMetadata)> GetCitiesAsync(string? name, string? searchQuery, int pageNumber, int pageSize)
       {
            //colletion to start from, we can add where clause
            var collection = _context.Cities as IQueryable<City>; //getCities igive araa rac es?

            if(!string.IsNullOrWhiteSpace(name)) 
            {
                name = name.Trim();
                collection =collection.Where(c=>c.Name == name);
            }
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = searchQuery.Trim();
                collection = collection.Where(a=>a.Name.Contains(searchQuery) || (a.Description != null && a.Description.Contains(searchQuery)));
            }
            var totalItemCount = await collection.CountAsync();

            var paginationMetadata = new PaginationMetadata(totalItemCount, pageSize, pageNumber); //ratom vqmnit axal instances xo tight couplengia

            var colletionToReturn =  await collection.OrderBy(c => c.Name)
                .Skip(pageSize * (pageNumber-1)) //Skip statement is attached to database level, we execute onfilted, searched and ordered colletion. That ensures that if for example gpage 2 is requested the amount of items on page 1 will be skipped
                .Take(pageSize) //take current requested pagesize
                .ToListAsync(); //query sent to database when ToListAsync() is reached. 

            return(colletionToReturn, paginationMetadata);
        }
        public async Task<City?> GetCityAsync(int cityId, bool includePointsOfInterest)
        {
            if(includePointsOfInterest)
            {
                return await _context.Cities.Include(c=>c.PointOfInterest).Where(c=>c.Id == cityId).FirstOrDefaultAsync();
            }

            return await _context.Cities.Where(c => c.Id == cityId).FirstOrDefaultAsync();
        }


        public async Task<bool> cityExistsAsync(int cityId)
        {
            return await _context.Cities.AnyAsync(c => c.Id == cityId);
        }
        public async Task<IEnumerable<PointOfInterest>> GetPointsOfInterestForCityAsync(int cityId)
        {
            return await _context.PointOfInterests.Where(p=>p.CityId == cityId).ToListAsync();
        }
        public async Task<PointOfInterest?> GetPointOfInterestForCityAsync(int cityId, int pointOfInterestId)
        {
            return await _context.PointOfInterests.Where(p => p.CityId == cityId && p.Id == pointOfInterestId).FirstOrDefaultAsync();
        }
        public async Task AddPointOfInterestForCityAsync(int cityId, PointOfInterest pointOfInterest) 
        {
            var city = await GetCityAsync(cityId, false);
            if(city != null)
            {
                city.PointOfInterest.Add(pointOfInterest); //adds it on the object context
            }
        }
        public void DeletePointOfInterest(PointOfInterest pointOfInterest)
        {
            _context.PointOfInterests.Remove(pointOfInterest);
        }
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() >= 0; //return the amount of entities that have been changed
        }

        public async Task<bool> CityNameMatchesCityId(string? cityName, int cityId)
        {
            return await _context.Cities.AnyAsync(c => c.Id == cityId && c.Name == cityName);
        }
    }
}
