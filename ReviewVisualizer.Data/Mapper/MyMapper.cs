using AutoMapper;
using ReviewVisualizer.Data.Models;
using ReviewVisualizer.Data.Dto;

namespace ReviewVisualizer.Data.Mapper
{
    public class MyMapper : Profile
    {
        public MyMapper()
        {
            CreateMap<Department, DepartmentCreateDTO>().ReverseMap();
            CreateMap<Teacher, TeacherCreateDTO>().ReverseMap();
            CreateMap<Reviewer, ReviewerCreateDTO>().ReverseMap();
            CreateMap<Analyst, AnalystCreateDTO>().ReverseMap();
        }
    }
}
