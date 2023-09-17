using System.Linq.Expressions;
using DBFeederEntity;

namespace DACService.Service
{
    public class GenericService<Te> where Te : BaseEntity
    {

        protected IUnitOfWork _unitOfWork;
        public GenericService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public GenericService()
        {
        }

        public virtual Guid Add(Te entity)
        {
            _unitOfWork.GetRepository<Te>().Insert(entity);
            _unitOfWork.Save();
            return entity.ID;
        }

        public virtual int Update(Te entity)
        {
            _unitOfWork.GetRepository<Te>().Update(entity.ID, entity);
            return _unitOfWork.Save();
        }


        public virtual int Remove(int id)
        {
            Te entity = _unitOfWork.Context.Set<Te>().Find(id);
            _unitOfWork.GetRepository<Te>().Delete(entity);
            return _unitOfWork.Save();
        }
    }
}
