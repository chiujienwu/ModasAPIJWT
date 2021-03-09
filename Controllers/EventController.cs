using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Modas.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Authorization;
using System;

namespace Modas.Controllers
{
    //## need to add login before adding back authorize##
    [Route("api/[controller]"), Authorize]
    // [Route("api/[controller]")]
    public class EventController : Controller
    {
        private EventDbContext eventDbContext;
        public EventController(EventDbContext db) => eventDbContext = db;

        [HttpGet, Route("count"), AllowAnonymous]
        // returns number of members in events collections
        public int GetCount() => eventDbContext.Events.Count();

        [HttpGet]
        // returns all events (sorted)
        public IEnumerable<Event> Get() => eventDbContext.Events
          .Include(e => e.Location).OrderBy(e => e.TimeStamp);

        [HttpGet("{id}")]
        // return specific event
        public Event Get(int id) => eventDbContext.Events
          .Include(e => e.Location)
          .FirstOrDefault(e => e.EventId == id);

        [HttpGet("pageSize/{pageSize:int}/page/{page:int}")]
        // returns all events by page
        public EventPage GetPage(int page = 1, int pageSize = 10) => new EventPage
        {
            Events = eventDbContext.Events
            .Select(e => new EventJson
            {
                id = e.EventId,
                flag = e.Flagged,
                stamp = e.TimeStamp,
                loc = e.Location.Name,
                locId = e.LocationId
            })
            .OrderByDescending(e => e.stamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize),
            PagingInfo = new PageInfo
            {
                CurrentPage = page,
                ItemsPerPage = pageSize,
                TotalItems = eventDbContext.Events.Count()
            },
            Locations = eventDbContext.Locations
            .Select(l => new LocJson
            {
                locName = l.Name,
                locId = l.LocationId
            })
        };

        [HttpGet("pageSize/{pageSize:int}/page/{page:int}/loc/{locval:int}")]
        // returns all events by page
        public EventPage GetFilteredPage(int page = 1, int pageSize = 10, int locval = 1)
        {
            int tempeventcount = eventDbContext.Events.Where(l => l.LocationId == locval).Count();
            double temppageestimate = (tempeventcount / pageSize);
            int pagecount = Convert.ToInt32(Math.Ceiling(temppageestimate));

            return new EventPage
            {
                Events = eventDbContext.Events
                    .Where(l => l.LocationId == locval)
                    .Select(e => new EventJson
                    {
                        id = e.EventId,
                        flag = e.Flagged,
                        stamp = e.TimeStamp,
                        loc = e.Location.Name,
                        locId = e.LocationId
                    })
                    .OrderByDescending(e => e.stamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize),
                PagingInfo = new PageInfo
                {
                    ItemsPerPage = pageSize,
                    TotalItems = eventDbContext.Events.Where(l => l.LocationId == locval).Count(),
                    CurrentPage = page > pagecount ? pagecount : page
                    //TotalPages = pagecount
                },
                Locations = eventDbContext.Locations
                    .Select(l => new LocJson
                    {
                        locName = l.Name,
                        locId = l.LocationId
                    })
            };
        }

        [HttpPost]
        // add event
        public Event Post([FromBody] Event evt) => eventDbContext.AddEvent(new Event
        {
            TimeStamp = evt.TimeStamp,
            Flagged = evt.Flagged,
            LocationId = evt.LocationId
        });
        [HttpPut]
        // update event
        public Event Put([FromBody] Event evt) => eventDbContext.UpdateEvent(evt);

        [HttpPatch("{id}")]
        // update event (specific fields)
        public void Patch(int id, [FromBody] JsonPatchDocument<Event> patch) => eventDbContext.PatchEvent(id, patch);

        [HttpDelete("{id}")]
        // delete event
        public void Delete(int id) => eventDbContext.DeleteEvent(id);

    }
}
