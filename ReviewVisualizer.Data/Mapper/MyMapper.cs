using AutoMapper;
using ReviewVisualizer.Data.Dto;
using ReviewVisualizer.Data.Models;

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
            CreateMap<Review, ReviewCreateDTO>().ReverseMap();
        }
    }
}