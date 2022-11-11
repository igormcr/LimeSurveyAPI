using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using PesquisaWebApi;

namespace PesquisaWebApi.Controllers
{
    public class tbl_pesquisa_usuarioController : ApiController
    {
        private isbet_pesquisa_desEntities db = new isbet_pesquisa_desEntities();

        // GET: api/tbl_pesquisa_usuario
        [Route("api/Test1")]
        public IQueryable<tbl_pesquisa_usuario> Gettbl_pesquisa_usuario()
        {
            return db.tbl_pesquisa_usuario;
        }

        // GET: api/tbl_pesquisa_usuario/5
        [Route("api/test2/$id")]
        [ResponseType(typeof(tbl_pesquisa_usuario))]
        public IHttpActionResult Gettbl_pesquisa_usuario(string id)
        {
            tbl_pesquisa_usuario tbl_pesquisa_usuario = db.tbl_pesquisa_usuario.Find(id);
            if (tbl_pesquisa_usuario == null)
            {
                return NotFound();
            }

            return Ok(tbl_pesquisa_usuario);
        }

        // PUT: api/tbl_pesquisa_usuario/5
        [ResponseType(typeof(void))]
        public IHttpActionResult Puttbl_pesquisa_usuario(int id, tbl_pesquisa_usuario tbl_pesquisa_usuario)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != tbl_pesquisa_usuario.id_envio_pesquisa)
            {
                return BadRequest();
            }

            db.Entry(tbl_pesquisa_usuario).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!tbl_pesquisa_usuarioExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/tbl_pesquisa_usuario
        [ResponseType(typeof(tbl_pesquisa_usuario))]
        public IHttpActionResult Posttbl_pesquisa_usuario(tbl_pesquisa_usuario tbl_pesquisa_usuario)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.tbl_pesquisa_usuario.Add(tbl_pesquisa_usuario);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = tbl_pesquisa_usuario.id_envio_pesquisa }, tbl_pesquisa_usuario);
        }

        // DELETE: api/tbl_pesquisa_usuario/5
        [ResponseType(typeof(tbl_pesquisa_usuario))]
        public IHttpActionResult Deletetbl_pesquisa_usuario(int id)
        {
            tbl_pesquisa_usuario tbl_pesquisa_usuario = db.tbl_pesquisa_usuario.Find(id);
            if (tbl_pesquisa_usuario == null)
            {
                return NotFound();
            }

            db.tbl_pesquisa_usuario.Remove(tbl_pesquisa_usuario);
            db.SaveChanges();

            return Ok(tbl_pesquisa_usuario);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool tbl_pesquisa_usuarioExists(int id)
        {
            return db.tbl_pesquisa_usuario.Count(e => e.id_envio_pesquisa == id) > 0;
        }
    }
}